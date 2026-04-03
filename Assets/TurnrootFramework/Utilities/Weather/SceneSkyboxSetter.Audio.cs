using UnityEngine;

namespace Turnroot.Utilities.Weather
{
    public partial class SceneSkyboxSetter : MonoBehaviour
    {
        #region Audio

        private AudioClip PickRandomClip(AudioClip[] clips) => clips == null || clips.Length == 0 ? null : clips[Random.Range(0, clips.Length)];

        private void UpdateAmbientAudio()
        {
            if (AmbientAudioSource == null)
            {
                return;
            }

            // If the weather hasn't changed and we have an active loop playing, leave it alone.
            if (
                AmbientAudioSource.clip != null
                && AmbientAudioSource.isPlaying
                && CurrentWeatherType == _lastAmbientWeatherType
            )
            {
                return;
            }

            _lastAmbientWeatherType = CurrentWeatherType;

            AudioClip clip = null;
            switch (CurrentWeatherType)
            {
                case WeatherType.Sunny:
                    clip = PickRandomClip(SunnyAmbientClips);
                    break;
                case WeatherType.Cloudy:
                    clip = PickRandomClip(CloudyAmbientClips);
                    break;
                case WeatherType.Rainy:
                    clip = PickRandomClip(RainyAmbientClips);
                    break;
                case WeatherType.Stormy:
                    clip = PickRandomClip(StormyAmbientClips);
                    break;
                case WeatherType.Volcanic:
                    clip = PickRandomClip(VolcanicAmbientClips);
                    break;
            }

            if (clip == null)
            {
                AmbientAudioSource.Stop();
                AmbientAudioSource.clip = null;
                return;
            }

            AmbientAudioSource.clip = clip;
            AmbientAudioSource.loop = true;
            AmbientAudioSource.Play();
        }

        private void ResetEventTimers()
        {
            float now = Time.time;
            _nextLightningTime = now + Random.Range(MinLightningInterval, MaxLightningInterval);
            _nextVolcanicRumbleTime =
                now + Random.Range(MinVolcanicRumbleInterval, MaxVolcanicRumbleInterval);
        }

        private void UpdateEventAudio()
        {
            if (CurrentWeatherType == WeatherType.Stormy)
            {
                TryTriggerLightningEvent();
            }
            else if (CurrentWeatherType == WeatherType.Volcanic)
            {
                TryTriggerVolcanicRumble();
            }
        }

        private void TryTriggerLightningEvent()
        {
            if (ThunderClips == null || ThunderClips.Length == 0 || EventAudioSource == null)
            {
                return;
            }

            float now = Time.time;
            if (now < _nextLightningTime)
            {
                return;
            }

            AudioClip clip = PickRandomClip(ThunderClips);
            if (clip == null)
            {
                ScheduleNextLightning();
                return;
            }

            Vector3 direction = Random.onUnitSphere;
            direction.y = Mathf.Abs(direction.y);
            if (direction.sqrMagnitude < 0.1f)
            {
                direction = Vector3.up;
            }

            direction.Normalize();

            SetEventAudioPosition(direction, EventSoundMinDistance, EventSoundMaxDistance);

            float intensity = 1f;
            if (currentSkybox != null && currentSkybox.HasProperty("_LightningIntensity"))
            {
                intensity = currentSkybox.GetFloat("_LightningIntensity");
            }

            SendLightningEventToShader(direction, intensity, _lightningDuration);

            EventAudioSource.PlayOneShot(clip);

            ScheduleNextLightning();
        }

        private void ScheduleNextLightning()
        {
            float freq = 0.5f;
            if (currentSkybox != null && currentSkybox.HasProperty("_LightningFrequency"))
            {
                freq = currentSkybox.GetFloat("_LightningFrequency");
                if (freq <= 0f)
                {
                    freq = 0.5f;
                }
            }

            float baseInterval = 1f / freq;
            baseInterval = Mathf.Clamp(baseInterval, MinLightningInterval, MaxLightningInterval);
            _nextLightningTime = Time.time + Random.Range(baseInterval * 0.8f, baseInterval * 1.2f);
        }

        private void TryTriggerVolcanicRumble()
        {
            if (
                VolcanicRumbleClips == null
                || VolcanicRumbleClips.Length == 0
                || EventAudioSource == null
            )
            {
                return;
            }

            float now = Time.time;
            if (now < _nextVolcanicRumbleTime)
            {
                return;
            }

            AudioClip clip = PickRandomClip(VolcanicRumbleClips);
            if (clip == null)
            {
                ScheduleNextVolcanicRumble();
                return;
            }

            Vector3 direction = Random.onUnitSphere;
            direction.y = Mathf.Abs(direction.y);
            if (direction.sqrMagnitude < 0.1f)
            {
                direction = Vector3.up;
            }

            direction.Normalize();

            SetEventAudioPosition(direction, EventSoundMinDistance, EventSoundMaxDistance);
            EventAudioSource.PlayOneShot(clip);

            // Optionally use a weak lightning event to drive sky flash
            SendLightningEventToShader(direction, 0.5f, .5f);

            ScheduleNextVolcanicRumble();
        }

        private void ScheduleNextVolcanicRumble()
        {
            _nextVolcanicRumbleTime =
                Time.time + Random.Range(MinVolcanicRumbleInterval, MaxVolcanicRumbleInterval);
        }

        private void SetEventAudioPosition(Vector3 direction, float minDistance, float maxDistance)
        {
            if (EventAudioSource == null)
            {
                return;
            }

            if (_audioListenerTransform == null)
            {
                _audioListenerTransform =
                    FindFirstObjectByType<AudioListener>()?.transform ?? Camera.main?.transform;
            }

            if (_audioListenerTransform == null)
            {
                return;
            }

            // Ensure the min/max are sane (non-negative, min <= max)
            minDistance = Mathf.Max(0.01f, minDistance);
            maxDistance = Mathf.Max(minDistance, maxDistance);

            float chosenDistance = Random.Range(minDistance, maxDistance);

            EventAudioSource.transform.position =
                _audioListenerTransform.position + direction.normalized * chosenDistance;
        }

        private void SendLightningEventToShader(Vector3 direction, float intensity, float duration)
        {
            var mat = currentSkybox ?? RenderSettings.skybox;
            if (mat == null)
            {
                return;
            }

            if (mat.HasProperty("_LightningEventStartTime"))
            {
                mat.SetFloat("_LightningEventStartTime", Time.time);
                mat.SetFloat("_LightningEventDuration", duration);
                mat.SetFloat("_LightningEventIntensity", intensity);
                mat.SetVector("_LightningEventDirection", direction);
            }
        }

        #endregion
    }
}
