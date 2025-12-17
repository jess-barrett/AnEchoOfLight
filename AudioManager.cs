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
        private static SoundEffect vaseBreakSound;
        private static SoundEffect dashSound;
        private static SoundEffect checkpointSound;
        private static SoundEffect dashGauntletMusicEffect;
        private static SoundEffect bossFightMusicEffect;
        private static SoundEffect bossFightVictorySound;

        // Orc Boss sounds
        private static SoundEffect orcAttackSound;
        private static SoundEffect orcHurtSound;
        private static SoundEffect orcIdleSound;
        private static SoundEffect orcJumpBackSound;
        private static SoundEffect orcPhaseChange1Sound;
        private static SoundEffect orcPhaseChange2Sound;
        private static SoundEffect orcPursueSound;
        private static SoundEffect orcDeathSound;
        private static SoundEffect playerTakeDamageSound;

        // Orc Boss sound instances (to prevent overlapping)
        private static SoundEffectInstance orcAttackInstance;
        private static SoundEffectInstance orcHurtInstance;
        private static SoundEffectInstance orcIdleInstance;
        private static SoundEffectInstance orcJumpBackInstance;
        private static SoundEffectInstance orcPhaseChange1Instance;
        private static SoundEffectInstance orcPhaseChange2Instance;
        private static SoundEffectInstance orcPursueInstance;
        private static SoundEffectInstance orcDeathInstance;
        private static SoundEffectInstance playerTakeDamageInstance;

        private static SoundEffectInstance menuMusicInstance;
        private static SoundEffectInstance gameplayMusicInstance;
        private static SoundEffectInstance dashGauntletMusicInstance;
        private static SoundEffectInstance bossFightMusicInstance;

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
                if (bossFightMusicInstance != null) bossFightMusicInstance.Volume = scaledVolume;
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
            vaseBreakSound = content.Load<SoundEffect>("Sound/VaseBreak");
            dashSound = content.Load<SoundEffect>("Sound/PlayerMovement/DashSound");
            checkpointSound = content.Load<SoundEffect>("Sound/CheckpointSound");
            dashGauntletMusicEffect = content.Load<SoundEffect>("Sound/DashGauntletSoundtrack");

            // Orc Boss sounds
            orcAttackSound = content.Load<SoundEffect>("Sound/Orc/OrcAttack");
            orcHurtSound = content.Load<SoundEffect>("Sound/Orc/OrcHurt");
            orcIdleSound = content.Load<SoundEffect>("Sound/Orc/OrcIdle");
            orcJumpBackSound = content.Load<SoundEffect>("Sound/Orc/OrcJumpBack");
            orcPhaseChange1Sound = content.Load<SoundEffect>("Sound/Orc/OrcPhaseChange1");
            orcPhaseChange2Sound = content.Load<SoundEffect>("Sound/Orc/OrcPhaseChange2");
            orcPursueSound = content.Load<SoundEffect>("Sound/Orc/OrcPursue");

            orcDeathSound = content.Load<SoundEffect>("Sound/Orc/OrcDeath");
            playerTakeDamageSound = content.Load<SoundEffect>("Sound/PlayerTakeDamage");

            // Create Orc sound instances
            orcAttackInstance = orcAttackSound.CreateInstance();
            orcHurtInstance = orcHurtSound.CreateInstance();
            orcIdleInstance = orcIdleSound.CreateInstance();
            orcJumpBackInstance = orcJumpBackSound.CreateInstance();
            orcPhaseChange1Instance = orcPhaseChange1Sound.CreateInstance();
            orcPhaseChange2Instance = orcPhaseChange2Sound.CreateInstance();
            orcPursueInstance = orcPursueSound.CreateInstance();
            orcDeathInstance = orcDeathSound.CreateInstance();
            playerTakeDamageInstance = playerTakeDamageSound.CreateInstance();

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

            bossFightMusicEffect = content.Load<SoundEffect>("Sound/BossFightSoundtrack");
            bossFightMusicInstance = bossFightMusicEffect.CreateInstance();
            bossFightMusicInstance.IsLooped = true;
            bossFightMusicInstance.Volume = musicVolume * 0.1f;

            bossFightVictorySound = content.Load<SoundEffect>("Sound/BossFightVictory");
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

            if (bossFightMusicInstance != null && bossFightMusicInstance.State == SoundState.Playing)
            {
                bossFightMusicInstance.Stop();
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

        public static void PlayBossFightMusic()
        {
            StopMusic();

            if (bossFightMusicInstance.State != SoundState.Playing)
            {
                bossFightMusicInstance.Play();
            }
        }

        public static bool IsBossFightMusicPlaying()
        {
            return bossFightMusicInstance != null && bossFightMusicInstance.State == SoundState.Playing;
        }

        public static void PlayBossFightVictorySound(float volume = 1f, float pitch = 0.0f, float pan = 0.0f)
        {
            bossFightVictorySound?.Play(volume * SFXVolume, pitch, pan);
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

        public static void PlayVaseBreakSound(float volume = 1f, float pitch = 0.0f, float pan = 0.0f)
        {
            vaseBreakSound?.Play(volume * SFXVolume, pitch, pan);
        }

        public static void PlayDashSound(float volume = 1f, float pitch = 0.0f, float pan = 0.0f)
        {
            dashSound?.Play(volume * SFXVolume, pitch, pan);
        }

        public static void PlayCheckpointSound(float volume = 1f, float pitch = 0.0f, float pan = 0.0f)
        {
            checkpointSound?.Play(volume * SFXVolume, pitch, pan);
        }

        // Orc Boss sound methods (using instances to prevent overlapping)
        public static void PlayOrcAttackSound(float volume = 1f, float pitch = 0.0f, float pan = 0.0f)
        {
            if (orcAttackInstance != null && orcAttackInstance.State != SoundState.Playing)
            {
                orcAttackInstance.Volume = volume * SFXVolume;
                orcAttackInstance.Pitch = pitch;
                orcAttackInstance.Pan = pan;
                orcAttackInstance.Play();
            }
        }

        public static void PlayOrcHurtSound(float volume = 1f, float pitch = 0.0f, float pan = 0.0f)
        {
            if (orcHurtInstance != null && orcHurtInstance.State != SoundState.Playing)
            {
                orcHurtInstance.Volume = volume * SFXVolume;
                orcHurtInstance.Pitch = pitch;
                orcHurtInstance.Pan = pan;
                orcHurtInstance.Play();
            }
        }

        public static void PlayOrcIdleSound(float volume = 1f, float pitch = 0.0f, float pan = 0.0f)
        {
            if (orcIdleInstance != null && orcIdleInstance.State != SoundState.Playing)
            {
                orcIdleInstance.Volume = volume * SFXVolume;
                orcIdleInstance.Pitch = pitch;
                orcIdleInstance.Pan = pan;
                orcIdleInstance.Play();
            }
        }

        public static void PlayOrcJumpBackSound(float volume = 1f, float pitch = 0.0f, float pan = 0.0f)
        {
            if (orcJumpBackInstance != null && orcJumpBackInstance.State != SoundState.Playing)
            {
                orcJumpBackInstance.Volume = volume * SFXVolume;
                orcJumpBackInstance.Pitch = pitch;
                orcJumpBackInstance.Pan = pan;
                orcJumpBackInstance.Play();
            }
        }

        public static void PlayOrcPhaseChange1Sound(float volume = 1f, float pitch = 0.0f, float pan = 0.0f)
        {
            if (orcPhaseChange1Instance != null && orcPhaseChange1Instance.State != SoundState.Playing)
            {
                orcPhaseChange1Instance.Volume = volume * SFXVolume;
                orcPhaseChange1Instance.Pitch = pitch;
                orcPhaseChange1Instance.Pan = pan;
                orcPhaseChange1Instance.Play();
            }
        }

        public static void PlayOrcPhaseChange2Sound(float volume = 1f, float pitch = 0.0f, float pan = 0.0f)
        {
            if (orcPhaseChange2Instance != null && orcPhaseChange2Instance.State != SoundState.Playing)
            {
                orcPhaseChange2Instance.Volume = volume * SFXVolume;
                orcPhaseChange2Instance.Pitch = pitch;
                orcPhaseChange2Instance.Pan = pan;
                orcPhaseChange2Instance.Play();
            }
        }

        public static void PlayOrcPursueSound(float volume = 1f, float pitch = 0.0f, float pan = 0.0f)
        {
            if (orcPursueInstance != null && orcPursueInstance.State != SoundState.Playing)
            {
                orcPursueInstance.Volume = volume * SFXVolume;
                orcPursueInstance.Pitch = pitch;
                orcPursueInstance.Pan = pan;
                orcPursueInstance.Play();
            }
        }

        public static void PlayOrcDeathSound(float volume = 1f, float pitch = 0.0f, float pan = 0.0f)
        {
            if (orcDeathInstance != null && orcDeathInstance.State != SoundState.Playing)
            {
                orcDeathInstance.Volume = volume * SFXVolume;
                orcDeathInstance.Pitch = pitch;
                orcDeathInstance.Pan = pan;
                orcDeathInstance.Play();
            }
        }

        public static void PlayPlayerTakeDamageSound(float volume = 1f, float pitch = 0.0f, float pan = 0.0f)
        {
            if (playerTakeDamageInstance != null && playerTakeDamageInstance.State != SoundState.Playing)
            {
                playerTakeDamageInstance.Volume = volume * SFXVolume;
                playerTakeDamageInstance.Pitch = pitch;
                playerTakeDamageInstance.Pan = pan;
                playerTakeDamageInstance.Play();
            }
        }
    }
}