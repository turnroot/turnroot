using System.Collections.Generic;
using Turnroot.Characters;
using Turnroot.Gameplay.Objects;
using UnityEngine.Events;

namespace Turnroot.Gameplay.Maps
{
    public partial class MapGridPoint
    {
        /* ---------------------------- Feature Property Accessors ---------------------------- */

        public void SetUnitFeatureProperty(string key, CharacterInstance value) =>
            SetProperty(_featureUnitProperties, key, value);

        public CharacterInstance GetUnitFeatureProperty(string key) =>
            GetProperty<MapGridPropertyBase.UnitProperty, CharacterInstance>(
                _featureUnitProperties,
                key
            );

        public List<MapGridPropertyBase.UnitProperty> GetAllUnitFeatureProperties() =>
            new(_featureUnitProperties);

        public void SetUnitPointProperty(string key, CharacterInstance value) =>
            SetProperty(_pointUnitProperties, key, value);

        public CharacterInstance GetUnitPointProperty(string key)
        {
            return GetProperty<MapGridPropertyBase.UnitProperty, CharacterInstance>(
                _pointUnitProperties,
                key
            );
        }

        public List<MapGridPropertyBase.UnitProperty> GetAllUnitPointProperties() =>
            new(_pointUnitProperties);

        public void SetObjectItemFeatureProperty(string key, ObjectItemInstance value) =>
            SetProperty(_featureObjectItemProperties, key, value);

        public ObjectItemInstance GetObjectItemFeatureProperty(string key) =>
            GetProperty<MapGridPropertyBase.ObjectItemProperty, ObjectItemInstance>(
                _featureObjectItemProperties,
                key
            );

        public List<MapGridPropertyBase.ObjectItemProperty> GetAllObjectItemFeatureProperties() =>
            new(_featureObjectItemProperties);

        public void SetObjectItemPointProperty(string key, ObjectItemInstance value) =>
            SetProperty(_pointObjectItemProperties, key, value);

        public ObjectItemInstance GetObjectItemPointProperty(string key) =>
            GetProperty<MapGridPropertyBase.ObjectItemProperty, ObjectItemInstance>(
                _pointObjectItemProperties,
                key
            );

        public List<MapGridPropertyBase.ObjectItemProperty> GetAllObjectItemPointProperties() =>
            new(_pointObjectItemProperties);

        // Bool properties
        public void SetBoolFeatureProperty(string key, bool value) =>
            SetProperty(_featureBoolProperties, key, value);

        public bool? GetBoolFeatureProperty(string key) =>
            GetNullableProperty<bool, MapGridPropertyBase.BoolProperty>(
                _featureBoolProperties,
                key
            );

        public List<MapGridPropertyBase.BoolProperty> GetAllBoolFeatureProperties() =>
            new(_featureBoolProperties);

        public void SetBoolPointProperty(string key, bool value) =>
            SetProperty(_pointBoolProperties, key, value);

        public bool? GetBoolPointProperty(string key) =>
            GetNullableProperty<bool, MapGridPropertyBase.BoolProperty>(_pointBoolProperties, key);

        public List<MapGridPropertyBase.BoolProperty> GetAllBoolPointProperties() =>
            new(_pointBoolProperties);

        public void SetFloatFeatureProperty(string key, float value) =>
            SetProperty(_featureFloatProperties, key, value);

        public float? GetFloatFeatureProperty(string key) =>
            GetNullableProperty<float, MapGridPropertyBase.FloatProperty>(
                _featureFloatProperties,
                key
            );

        public List<MapGridPropertyBase.FloatProperty> GetAllFloatFeatureProperties() =>
            new(_featureFloatProperties);

        public void SetFloatPointProperty(string key, float value) =>
            SetProperty(_pointFloatProperties, key, value);

        public float? GetFloatPointProperty(string key) =>
            GetNullableProperty<float, MapGridPropertyBase.FloatProperty>(
                _pointFloatProperties,
                key
            );

        public List<MapGridPropertyBase.FloatProperty> GetAllFloatPointProperties() =>
            new(_pointFloatProperties);

        public void SetEventFeatureProperty(string key, UnityEvent value) =>
            SetProperty(_featureEventProperties, key, value);

        public UnityEvent GetEventFeatureProperty(string key)
        {
            return GetProperty<MapGridPropertyBase.EventProperty, UnityEvent>(
                _featureEventProperties,
                key
            );
        }

        public List<MapGridPropertyBase.EventProperty> GetAllEventFeatureProperties() =>
            new(_featureEventProperties);

        public void SetEventPointProperty(string key, UnityEvent value) =>
            SetProperty(_pointEventProperties, key, value);

        public UnityEvent GetEventPointProperty(string key)
        {
            return GetProperty<MapGridPropertyBase.EventProperty, UnityEvent>(
                _pointEventProperties,
                key
            );
        }

        public List<MapGridPropertyBase.EventProperty> GetAllEventPointProperties() =>
            new(_pointEventProperties);
    }
}
