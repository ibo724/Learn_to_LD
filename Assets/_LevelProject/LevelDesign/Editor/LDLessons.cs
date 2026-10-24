using System.Collections.Generic;

/// <summary>Every lesson the window offers, in teaching order.</summary>
public static class LDLessons
{
    public static IReadOnlyList<LDLesson> All { get; } = new[] { Ders01Arena.Create(LDLessonSceneSetup.CreateOrOpen) };
}
