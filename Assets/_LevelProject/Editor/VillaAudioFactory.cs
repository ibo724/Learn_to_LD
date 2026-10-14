#if UNITY_EDITOR
using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace VillaFPS.Editor
{
    /// <summary>Small deterministic effects for the prototype; no recordings or external media.</summary>
    public static class VillaAudioFactory
    {
        public const string AudioFolder = "Assets/_LevelProject/FPS/Audio/Generated";
        private const int SampleRate = 22050;
        private const float TwoPi = Mathf.PI * 2f;

        private static readonly string[] Names =
        {
            "VO_Generated_00_CarbineShot", "VO_Generated_01_Reload", "VO_Generated_02_Hurt",
            "VO_Generated_03_DataCollected", "VO_Generated_04_Pickup", "VO_Generated_05_EmptyTrigger",
            "VO_Generated_06_Footstep", "VO_Generated_07_GuardAlert", "VO_Generated_08_Success",
            "VO_Generated_09_Failure", "VO_Generated_10_HitConfirmation"
        };

        // Order: shot, reload, hurt, data, pickup, empty, footstep, alert, success, failure, hit.
        public static AudioClip[] CreateCues()
        {
            EnsureFolder(AudioFolder);
            AudioClip[] clips = new AudioClip[Names.Length];
            string root = Directory.GetParent(Application.dataPath).FullName;
            for (int i = 0; i < Names.Length; i++)
            {
                string path = AudioFolder + "/" + Names[i] + ".wav";
                string diskPath = Path.Combine(root, path.Replace('/', Path.DirectorySeparatorChar));
                bool created = false;
                if (!File.Exists(diskPath))
                {
                    float[] samples = Synthesize(i);
                    FinishSamples(samples);
                    created = WriteWave(diskPath, samples);
                }

                if (created || AssetDatabase.LoadAssetAtPath<AudioClip>(path) == null)
                    AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
                ConfigureImporter(path);
                clips[i] = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
                if (clips[i] == null)
                    throw new InvalidOperationException("Generated cue could not be imported: " + path);
            }
            return clips;
        }

        private static void EnsureFolder(string path)
        {
            string[] parts = path.Split('/');
            string current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next)) AssetDatabase.CreateFolder(current, parts[i]);
                current = next;
            }
        }

        private static void ConfigureImporter(string path)
        {
            AudioImporter importer = AssetImporter.GetAtPath(path) as AudioImporter;
            if (importer == null) throw new InvalidOperationException("Missing audio importer: " + path);
            bool changed = false;
            if (!importer.forceToMono) { importer.forceToMono = true; changed = true; }
            if (importer.loadInBackground) { importer.loadInBackground = false; changed = true; }
            if (importer.ambisonic) { importer.ambisonic = false; changed = true; }

            AudioImporterSampleSettings settings = importer.defaultSampleSettings;
            if (settings.loadType != AudioClipLoadType.DecompressOnLoad ||
                settings.compressionFormat != AudioCompressionFormat.PCM ||
                settings.sampleRateSetting != AudioSampleRateSetting.PreserveSampleRate ||
                !settings.preloadAudioData || !Mathf.Approximately(settings.quality, 1f))
            {
                settings.loadType = AudioClipLoadType.DecompressOnLoad;
                settings.compressionFormat = AudioCompressionFormat.PCM;
                settings.sampleRateSetting = AudioSampleRateSetting.PreserveSampleRate;
                settings.preloadAudioData = true;
                settings.quality = 1f;
                importer.defaultSampleSettings = settings;
                changed = true;
            }

            // Normalize has no public AudioImporter property in Unity 6. Preserve cue gain relationships.
            SerializedObject serialized = new SerializedObject(importer);
            SerializedProperty normalize = serialized.FindProperty("m_Normalize");
            if (normalize != null && normalize.propertyType == SerializedPropertyType.Boolean && normalize.boolValue)
            {
                normalize.boolValue = false;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                changed = true;
            }
            if (changed) importer.SaveAndReimport();
        }

        private static float[] Synthesize(int cue)
        {
            Noise noise = new Noise(0x6D2B79F5u + (uint)cue * 7919u);
            switch (cue)
            {
                case 0: return Shot(noise);
                case 1: return Reload(noise);
                case 2: return Hurt(noise);
                case 3: return Arpeggio(.34f, new[] { 293.66f, 369.99f, 440f, 587.33f }, .055f, .16f, .26f);
                case 4: return Arpeggio(.23f, new[] { 587.33f, 880f }, .07f, .15f, .23f);
                case 5: return Empty(noise);
                case 6: return Footstep(noise);
                case 7: return Alert();
                case 8: return Arpeggio(.72f, new[] { 293.66f, 369.99f, 440f, 587.33f }, .14f, .28f, .30f);
                case 9: return Arpeggio(.55f, new[] { 440f, 349.23f, 293.66f }, .14f, .26f, .25f);
                case 10: return HitConfirmation(noise);
                default: throw new ArgumentOutOfRangeException(nameof(cue));
            }
        }

        private static float[] Shot(Noise noise)
        {
            float[] samples = Buffer(.105f);
            float low = 0f;
            for (int i = 0; i < samples.Length; i++)
            {
                float t = i / (float)SampleRate;
                float raw = noise.Next();
                low += .18f * (raw - low);
                float phase = TwoPi * (78f * t + 90f * .012f * (1f - Mathf.Exp(-t / .012f)));
                float body = Mathf.Sin(phase) * Envelope(t, .001f, .030f) * .62f;
                float crack = (raw - low * .65f) * Envelope(t, .0004f, .014f) * .70f;
                float tail = low * Envelope(t, .001f, .035f) * .18f;
                samples[i] = body + crack + tail;
            }
            return samples;
        }

        private static float[] Reload(Noise noise)
        {
            float[] samples = Buffer(.43f);
            for (int i = 0; i < samples.Length; i++)
            {
                float t = i / (float)SampleRate;
                float raw = noise.Next();
                samples[i] = Clink(t - .012f, raw, 1740f, .24f) +
                             Clink(t - .173f, raw, 2350f, .20f) +
                             Clink(t - .317f, raw, 1180f, .28f);
                float close = t - .317f;
                if (close >= 0f) samples[i] += Mathf.Sin(TwoPi * 210f * close) * Envelope(close, .001f, .027f) * .22f;
            }
            return samples;
        }

        private static float Clink(float time, float raw, float pitch, float gain)
        {
            if (time < 0f) return 0f;
            float metal = Mathf.Sin(TwoPi * pitch * time) + Mathf.Sin(TwoPi * pitch * 1.487f * time) * .38f;
            return gain * (metal * Envelope(time, .0008f, .021f) + raw * Envelope(time, .0005f, .009f) * .75f);
        }

        private static float[] Hurt(Noise noise)
        {
            float[] samples = Buffer(.17f);
            float low = 0f;
            for (int i = 0; i < samples.Length; i++)
            {
                float t = i / (float)SampleRate;
                low += .10f * (noise.Next() - low);
                float phase = TwoPi * (83f * t + 52f * .018f * (1f - Mathf.Exp(-t / .018f)));
                samples[i] = (Mathf.Sin(phase) * .47f + low * .42f) * Envelope(t, .0017f, .042f);
            }
            return samples;
        }

        private static float[] Empty(Noise noise)
        {
            float[] samples = Buffer(.055f);
            for (int i = 0; i < samples.Length; i++)
            {
                float t = i / (float)SampleRate;
                samples[i] = (noise.Next() * .23f + Mathf.Sin(TwoPi * 1450f * t) * .14f) * Envelope(t, .0005f, .007f);
            }
            return samples;
        }

        private static float[] Footstep(Noise noise)
        {
            float[] samples = Buffer(.11f);
            float low = 0f;
            for (int i = 0; i < samples.Length; i++)
            {
                float t = i / (float)SampleRate;
                low += .16f * (noise.Next() - low);
                samples[i] = low * Envelope(t, .0015f, .027f) * .45f +
                             Mathf.Sin(TwoPi * 125f * t) * Envelope(t, .001f, .017f) * .16f;
            }
            return samples;
        }

        private static float[] HitConfirmation(Noise noise)
        {
            float[] samples = Buffer(.075f);
            float low = 0f;
            for (int i = 0; i < samples.Length; i++)
            {
                float t = i / (float)SampleRate;
                low += .24f * (noise.Next() - low);
                float metal = Mathf.Sin(TwoPi * 1880f * t) * .21f +
                              Mathf.Sin(TwoPi * 2770f * t) * .08f;
                samples[i] = metal * Envelope(t, .0012f, .014f) +
                             low * Envelope(t, .0008f, .006f) * .12f;
            }
            return samples;
        }

        private static float[] Alert()
        {
            float[] samples = Buffer(.29f);
            for (int i = 0; i < samples.Length; i++)
            {
                float t = i / (float)SampleRate;
                samples[i] = Chirp(t - .01f, 440f, 660f, .085f) + Chirp(t - .15f, 520f, 780f, .085f);
            }
            return samples;
        }

        private static float Chirp(float t, float start, float end, float duration)
        {
            if (t < 0f || t > duration) return 0f;
            float phase = TwoPi * (start * t + (end - start) * t * t / (2f * duration));
            float fade = Mathf.Sin(Mathf.PI * t / duration);
            return Mathf.Sin(phase) * fade * fade * .23f;
        }

        private static float[] Arpeggio(float duration, float[] notes, float spacing, float noteDuration, float gain)
        {
            float[] samples = Buffer(duration);
            for (int note = 0; note < notes.Length; note++)
            {
                int start = Mathf.RoundToInt(note * spacing * SampleRate);
                int end = Mathf.Min(samples.Length, start + Mathf.CeilToInt(noteDuration * SampleRate));
                for (int i = start; i < end; i++)
                {
                    float t = (i - start) / (float)SampleRate;
                    float phase = TwoPi * notes[note] * t;
                    float warm = Mathf.Sin(phase) * .75f + Mathf.Sin(phase * 2f) * .17f + Mathf.Sin(phase * 3f) * .06f;
                    float attack = Mathf.Clamp01(t / .004f);
                    float release = Mathf.Clamp01((noteDuration - t) / .065f);
                    float decay = Mathf.Exp(-t / (noteDuration * .8f));
                    samples[i] += warm * attack * release * decay * gain;
                }
            }
            return samples;
        }

        private static float Envelope(float t, float attack, float decay)
        {
            if (t < 0f) return 0f;
            return (1f - Mathf.Exp(-t / attack)) * Mathf.Exp(-t / decay);
        }

        private static float[] Buffer(float duration) => new float[Mathf.CeilToInt(duration * SampleRate)];

        private static void FinishSamples(float[] samples)
        {
            double total = 0;
            for (int i = 0; i < samples.Length; i++) total += samples[i];
            float mean = (float)(total / samples.Length);
            float peak = 0f;
            for (int i = 0; i < samples.Length; i++)
            {
                float time = i / (float)SampleRate;
                float remaining = (samples.Length - 1 - i) / (float)SampleRate;
                float fade = Mathf.Clamp01(time / .001f) * Mathf.Clamp01(remaining / .008f);
                samples[i] = (samples[i] - mean) * fade;
                peak = Mathf.Max(peak, Mathf.Abs(samples[i]));
            }
            // Reduce only: quiet footsteps remain quieter than weapon fire.
            float headroom = peak > .84f ? .84f / peak : 1f;
            for (int i = 0; i < samples.Length; i++) samples[i] = Mathf.Clamp(samples[i] * headroom, -.84f, .84f);
        }

        private static bool WriteWave(string path, float[] samples)
        {
            int bytes = checked(samples.Length * sizeof(short));
            FileStream stream;
            // Only an opening collision is recoverable; writing errors must reach the builder.
            try { stream = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.Read); }
            catch (IOException) { if (File.Exists(path)) return false; throw; }
            try
            {
                using (stream)
                using (BinaryWriter writer = new BinaryWriter(stream, Encoding.ASCII))
                {
                    writer.Write(Encoding.ASCII.GetBytes("RIFF"));
                    writer.Write(36 + bytes);
                    writer.Write(Encoding.ASCII.GetBytes("WAVE"));
                    writer.Write(Encoding.ASCII.GetBytes("fmt "));
                    writer.Write(16);
                    writer.Write((short)1); // Linear PCM.
                    writer.Write((short)1); // Mono.
                    writer.Write(SampleRate);
                    writer.Write(SampleRate * sizeof(short));
                    writer.Write((short)sizeof(short));
                    writer.Write((short)16);
                    writer.Write(Encoding.ASCII.GetBytes("data"));
                    writer.Write(bytes);
                    for (int i = 0; i < samples.Length; i++) writer.Write((short)Mathf.RoundToInt(samples[i] * short.MaxValue));
                }
            }
            catch
            {
                // This method owns the newly created file; never leave a truncated clip behind.
                if (File.Exists(path)) File.Delete(path);
                throw;
            }
            return true;
        }

        // Explicit xorshift keeps output independent of System.Random implementation changes.
        private sealed class Noise
        {
            private uint state;
            public Noise(uint seed) { state = seed == 0 ? 1u : seed; }
            public float Next()
            {
                state ^= state << 13;
                state ^= state >> 17;
                state ^= state << 5;
                return (state >> 8) * (2f / 16777215f) - 1f;
            }
        }
    }
}
#endif
