using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;

namespace GameProject2
{
    public static class AudioManager
    {
        private static SoundEffect menuMusicEffect;
        private static SoundEffect gameplayMusicEffect;
        private static SoundEffect menuIntro;
        private static SoundEffect gameplayIntro;
        private static SoundEffect footstepSound;
        private static SoundEffect deathSound;
        private static SoundEffect swingSwordSound;
        private static SoundEffect takeDamageSound;

        private static SoundEffectInstance menuMusicInstance;
        private static SoundEffectInstance gameplayMusicInstance;

        private static float musicVolume = 0.5f;
        private static float sfxVolume = 1.0f;

        public static float MusicVolume
        {
            get => musicVolume;
            set
            {
                musicVolume = MathHelper.Clamp(value, 0f, 1f);
                float scaledVolume = musicVolume * 0.1f;
                if (menuMusicInstance != null) menuMusicInstance.Volume = scaledVolume;
                if (gameplayMusicInstance != null) gameplayMusicInstance.Volume = scaledVolume;
            }
        }

        public static float SFXVolume
        {
            get => sfxVolume;
            set
            {
                sfxVolume = MathHelper.Clamp(value, 0f, 1f);
            }
        }

        public static void LoadContent(ContentManager content)
        {
            menuIntro = content.Load<SoundEffect>("Sound/IntroJingle");
            menuMusicEffect = content.Load<SoundEffect>("Sound/Venus");
            gameplayIntro = content.Load<SoundEffect>("Sound/BossIntro");
            gameplayMusicEffect = content.Load<SoundEffect>("Sound/BossMain");
            footstepSound = content.Load<SoundEffect>("Sound/PlayerMovement/03_Step_grass_03");
            deathSound = content.Load<SoundEffect>("Sound/Death");
            swingSwordSound = content.Load<SoundEffect>("Sound/SwingSword");
            takeDamageSound = content.Load<SoundEffect>("Sound/TakeDamage");

            // Create looping instances for music
            menuMusicInstance = menuMusicEffect.CreateInstance();
            menuMusicInstance.IsLooped = true;
            menuMusicInstance.Volume = musicVolume * 0.1f;

            gameplayMusicInstance = gameplayMusicEffect.CreateInstance();
            gameplayMusicInstance.IsLooped = true;
            gameplayMusicInstance.Volume = musicVolume * 0.1f;
        }

        public static void PlayMenuMusicWithIntro()
        {
            StopMusic();

            var introInstance = menuIntro.CreateInstance();
            introInstance.Volume = musicVolume * 0.1f;
            introInstance.Play();

            if (menuMusicInstance.State != SoundState.Playing)
            {
                menuMusicInstance.Play();
            }
        }

        public static void PlayGameplayMusicWithIntro()
        {
            StopMusic();

            var introInstance = gameplayIntro.CreateInstance();
            introInstance.Volume = musicVolume * 0.1f;
            introInstance.Play();

            if (gameplayMusicInstance.State != SoundState.Playing)
            {
                gameplayMusicInstance.Play();
            }
        }

        public static void PlayMenuMusic()
        {
            StopMusic();

            if (menuMusicInstance.State != SoundState.Playing)
            {
                menuMusicInstance.Play();
            }
        }

        public static void PlayGameplayMusic()
        {
            StopMusic();

            if (gameplayMusicInstance.State != SoundState.Playing)
            {
                gameplayMusicInstance.Play();
            }
        }

        public static void StopMusic()
        {
            if (menuMusicInstance != null && menuMusicInstance.State == SoundState.Playing)
            {
                menuMusicInstance.Stop();
            }

            if (gameplayMusicInstance != null && gameplayMusicInstance.State == SoundState.Playing)
            {
                gameplayMusicInstance.Stop();
            }
        }

        public static void PlayFootstep(float volume, float pitch)
        {
            if (footstepSound != null)
            {
                footstepSound.Play(volume * sfxVolume, pitch, 0f);
            }
        }

        public static void PlayDeathSound(float volume = 1f, float pitch = 0.0f, float pan = 0.0f)
        {
            deathSound?.Play(volume * SFXVolume, pitch, pan);
        }

        public static void PlaySwingSwordSound(float volume = 1f, float pitch = 0.0f, float pan = 0.0f)
        {
            swingSwordSound?.Play(volume * SFXVolume, pitch, pan);
        }

        public static void PlayTakeDamageSound(float volume = 1f, float pitch = 0.0f, float pan = 0.0f)
        {
            takeDamageSound?.Play(volume * SFXVolume, pitch, pan);
        }
    }
}