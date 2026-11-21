namespace LastStopNight.Game;

/// <summary>Un sunet cerut de scena (efect, pas): numele fisierului, volumul (0..1) si viteza de redare.</summary>
public readonly record struct SoundCue(string Name, float Volume = 1, float Rate = 1);
