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
        private static SoundEffect healSound;
        private static SoundEffect attackLandingSound;
        private static SoundEffect totemActivateSound;
        private static SoundEffect totemSpawnEnemySound;
        private static SoundEffect totemAbilityUnlockSound;
        private static SoundEffect totemDestroySound;
        private static SoundEffect gauntletDefeatedSound;
        private static SoundEffect woodDoorOpenSound;
        private static SoundEffect chestOpenSound;
        private static SoundEffect potionPickupSound;
        private static SoundEffect coinPickupSound;
        private static SoundEffect dashGauntletMusicEffect;

        private static SoundEffectInstance menuMusicInstance;
        private static SoundEffectInstance gameplayMusicInstance;
        private static SoundEffectInstance dashGauntletMusicInstance;

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
                if (dashGauntletMusicInstance != null) dashGauntletMusicInstance.Volume = scaledVolume;
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
            healSound = content.Load<SoundEffect>("Sound/HealSound");
            attackLandingSound = content.Load<SoundEffect>("Sound/AttackLanding");
            totemActivateSound = content.Load<SoundEffect>("Sound/TotemActivateSound");
            totemSpawnEnemySound = content.Load<SoundEffect>("Sound/TotemSpawnEnemySound");
            totemAbilityUnlockSound = content.Load<SoundEffect>("Sound/TotemAbilityUnlockSound");
            totemDestroySound = content.Load<SoundEffect>("Sound/TotemDestroySound");
            gauntletDefeatedSound = content.Load<SoundEffect>("Sound/GauntletDefeatedSound");
            woodDoorOpenSound = content.Load<SoundEffect>("Sound/WoodDoorOpenSound");
            chestOpenSound = content.Load<SoundEffect>("Sound/ChestOpenSound");
            potionPickupSound = content.Load<SoundEffect>("Sound/PotionPickupSound");
            coinPickupSound = content.Load<SoundEffect>("Sound/CoinPickupSound");
            dashGauntletMusicEffect = content.Load<SoundEffect>("Sound/DashGauntletSoundtrack");

            // Create looping instances for music
            menuMusicInstance = menuMusicEffect.CreateInstance();
            menuMusicInstance.IsLooped = true;
            menuMusicInstance.Volume = musicVolume * 0.1f;

            gameplayMusicInstance = gameplayMusicEffect.CreateInstance();
            gameplayMusicInstance.IsLooped = true;
            gameplayMusicInstance.Volume = musicVolume * 0.1f;

            dashGauntletMusicInstance = dashGauntletMusicEffect.CreateInstance();
            dashGauntletMusicInstance.IsLooped = true;
            dashGauntletMusicInstance.Volume = musicVolume * 0.1f;
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

            if (dashGauntletMusicInstance != null && dashGauntletMusicInstance.State == SoundState.Playing)
            {
                dashGauntletMusicInstance.Stop();
            }
        }

        public static void PlayDashGauntletMusic()
        {
            StopMusic();

            if (dashGauntletMusicInstance.State != SoundState.Playing)
            {
                dashGauntletMusicInstance.Play();
            }
        }

        public static bool IsDashGauntletMusicPlaying()
        {
            return dashGauntletMusicInstance != null && dashGauntletMusicInstance.State == SoundState.Playing;
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

        public static void PlayHealSound(float volume = 1f, float pitch = 0.0f, float pan = 0.0f)
        {
            healSound?.Play(volume * SFXVolume, pitch, pan);
        }

        public static void PlayAttackLandingSound(float volume = 1f, float pitch = 0.0f, float pan = 0.0f)
        {
            attackLandingSound?.Play(volume * SFXVolume, pitch, pan);
        }

        public static void PlayTotemActivateSound(float volume = 1f, float pitch = 0.0f, float pan = 0.0f)
        {
            totemActivateSound?.Play(volume * SFXVolume, pitch, pan);
        }

        public static void PlayTotemSpawnEnemySound(float volume = 1f, float pitch = 0.0f, float pan = 0.0f)
        {
            totemSpawnEnemySound?.Play(volume * SFXVolume, pitch, pan);
        }

        public static void PlayTotemAbilityUnlockSound(float volume = 1f, float pitch = 0.0f, float pan = 0.0f)
        {
            totemAbilityUnlockSound?.Play(volume * SFXVolume, pitch, pan);
        }

        public static void PlayTotemDestroySound(float volume = 1f, float pitch = 0.0f, float pan = 0.0f)
        {
            totemDestroySound?.Play(volume * SFXVolume, pitch, pan);
        }

        public static void PlayGauntletDefeatedSound(float volume = 1f, float pitch = 0.0f, float pan = 0.0f)
        {
            gauntletDefeatedSound?.Play(volume * SFXVolume, pitch, pan);
        }

        public static void PlayWoodDoorOpenSound(float volume = 1f, float pitch = 0.0f, float pan = 0.0f)
        {
            woodDoorOpenSound?.Play(volume * SFXVolume, pitch, pan);
        }

        public static void PlayChestOpenSound(float volume = 1f, float pitch = 0.0f, float pan = 0.0f)
        {
            chestOpenSound?.Play(volume * SFXVolume, pitch, pan);
        }

        public static void PlayPotionPickupSound(float volume = 1f, float pitch = 0.0f, float pan = 0.0f)
        {
            potionPickupSound?.Play(volume * SFXVolume, pitch, pan);
        }

        public static void PlayCoinPickupSound(float volume = 1f, float pitch = 0.0f, float pan = 0.0f)
        {
            coinPickupSound?.Play(volume * SFXVolume, pitch, pan);
        }
    }
}