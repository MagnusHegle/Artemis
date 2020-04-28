﻿using System;
using System.Collections.Generic;
using System.Linq;
using Artemis.Core.Exceptions;
using Artemis.Core.Models.Profile.KeyframeEngines;
using Artemis.Core.Models.Profile.LayerProperties.Abstract;
using Artemis.Core.Plugins.Models;
using Artemis.Core.Utilities;
using Artemis.Storage.Entities.Profile;
using Newtonsoft.Json;
using Stylet;

namespace Artemis.Core.Models.Profile.LayerProperties
{
    public abstract class BaseLayerProperty : PropertyChangedBase
    {
        private object _baseValue;
        private bool _isHidden;

        protected BaseLayerProperty(Layer layer, PluginInfo pluginInfo, BaseLayerProperty parent, string id, string name, string description, Type type)
        {
            Layer = layer;
            PluginInfo = pluginInfo;
            Parent = parent;
            Id = id;
            Name = name;
            Description = description;
            Type = type;
            CanUseKeyframes = true;
            InputStepSize = 1;

            // This can only be null if accessed internally, all public ways of creating enforce a plugin info
            if (PluginInfo == null)
                PluginInfo = Constants.CorePluginInfo;

            Children = new List<BaseLayerProperty>();
            BaseKeyframes = new List<BaseKeyframe>();


            parent?.Children.Add(this);
        }

        /// <summary>
        ///     Gets the layer this property applies to
        /// </summary>
        public Layer Layer { get; }

        /// <summary>
        ///     Info of the plugin associated with this property
        /// </summary>
        public PluginInfo PluginInfo { get; }

        /// <summary>
        ///     Gets the parent property of this property.
        /// </summary>
        public BaseLayerProperty Parent { get; }

        /// <summary>
        ///     Gets or sets the child properties of this property.
        ///     <remarks>If the layer has children it cannot contain a value or keyframes.</remarks>
        /// </summary>
        public List<BaseLayerProperty> Children { get; set; }

        /// <summary>
        ///     Gets or sets a unique identifier for this property, a layer may not contain two properties with the same ID.
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        ///     Gets or sets the user-friendly name for this property, shown in the UI.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        ///     Gets or sets the user-friendly description for this property, shown in the UI.
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        ///     Gets or sets whether to expand this property by default, this is useful for important parent properties.
        /// </summary>
        public bool ExpandByDefault { get; set; }

        /// <summary>
        ///     Gets or sets the an optional input prefix to show before input elements in the UI.
        /// </summary>
        public string InputPrefix { get; set; }

        /// <summary>
        ///     Gets or sets an optional input affix to show behind input elements in the UI.
        /// </summary>
        public string InputAffix { get; set; }

        /// <summary>
        ///     Gets or sets an optional maximum input value, only enforced in the UI.
        /// </summary>
        public object MaxInputValue { get; set; }

        /// <summary>
        ///     Gets or sets the input drag step size, used in the UI.
        /// </summary>
        public float InputStepSize { get; set; }

        /// <summary>
        ///     Gets or sets an optional minimum input value, only enforced in the UI.
        /// </summary>
        public object MinInputValue { get; set; }

        /// <summary>
        ///     Gets or sets whether this property can use keyframes, True by default.
        /// </summary>
        public bool CanUseKeyframes { get; set; }

        /// <summary>
        ///     Gets or sets whether this property is using keyframes.
        /// </summary>
        public bool IsUsingKeyframes { get; set; }

        /// <summary>
        ///     Gets the type of value this layer property contains.
        /// </summary>
        public Type Type { get; protected set; }

        /// <summary>
        ///     Gets or sets whether this property is hidden in the UI.
        /// </summary>
        public bool IsHidden
        {
            get => _isHidden;
            set
            {
                _isHidden = value;
                OnVisibilityChanged();
            }
        }

        /// <summary>
        ///     Gets a list of keyframes defining different values of the property in time, this list contains the untyped
        ///     <see cref="BaseKeyframe" />.
        /// </summary>
        public IReadOnlyCollection<BaseKeyframe> UntypedKeyframes => BaseKeyframes.AsReadOnly();

        /// <summary>
        ///     Gets the keyframe engine instance of this property
        /// </summary>
        public KeyframeEngine KeyframeEngine { get; internal set; }

        protected List<BaseKeyframe> BaseKeyframes { get; set; }

        public object BaseValue
        {
            get => _baseValue;
            internal set
            {
                if (value != null && value.GetType() != Type)
                    throw new ArtemisCoreException($"Cannot set value of type {value.GetType()} on property {this}, expected type is {Type}.");
                if (!Equals(_baseValue, value))
                {
                    _baseValue = value;
                    OnValueChanged();
                }
            }
        }

        /// <summary>
        ///     Creates a new keyframe for this base property without knowing the type
        /// </summary>
        /// <returns></returns>
        public BaseKeyframe CreateNewKeyframe(TimeSpan position, object value)
        {
            // Create a strongly typed keyframe or else it cannot be cast later on
            var keyframeType = typeof(Keyframe<>);
            var keyframe = (BaseKeyframe) Activator.CreateInstance(keyframeType.MakeGenericType(Type), Layer, this);
            keyframe.Position = position;
            keyframe.BaseValue = value;
            BaseKeyframes.Add(keyframe);
            SortKeyframes();

            return keyframe;
        }

        /// <summary>
        ///     Removes all keyframes from the property and sets the base value to the current value.
        /// </summary>
        public void ClearKeyframes()
        {
            if (KeyframeEngine != null)
                BaseValue = KeyframeEngine.GetCurrentValue();

            BaseKeyframes.Clear();
        }

        /// <summary>
        ///     Gets the current value using the regular value or if present, keyframes
        /// </summary>
        public object GetCurrentValue()
        {
            if (KeyframeEngine == null || !UntypedKeyframes.Any())
                return BaseValue;

            return KeyframeEngine.GetCurrentValue();
        }

        /// <summary>
        ///     Gets the current value using the regular value or keyframes.
        /// </summary>
        /// <param name="value">The value to set.</param>
        /// <param name="time">
        ///     An optional time to set the value add, if provided and property is using keyframes the value will be set to an new
        ///     or existing keyframe.
        /// </param>
        public void SetCurrentValue(object value, TimeSpan? time)
        {
            if (value != null && value.GetType() != Type)
                throw new ArtemisCoreException($"Cannot set value of type {value.GetType()} on property {this}, expected type is {Type}.");

            if (time == null || !CanUseKeyframes || !IsUsingKeyframes)
                BaseValue = value;
            else
            {
                // If on a keyframe, update the keyframe
                var currentKeyframe = UntypedKeyframes.FirstOrDefault(k => k.Position == time.Value);
                // Create a new keyframe if none found
                if (currentKeyframe == null)
                    currentKeyframe = CreateNewKeyframe(time.Value, value);

                currentKeyframe.BaseValue = value;
            }

            OnValueChanged();
        }

        /// <summary>
        ///     Adds a keyframe to the property.
        /// </summary>
        /// <param name="keyframe">The keyframe to remove</param>
        public void AddKeyframe(BaseKeyframe keyframe)
        {
            BaseKeyframes.Add(keyframe);
            SortKeyframes();
        }

        /// <summary>
        ///     Removes a keyframe from the property.
        /// </summary>
        /// <param name="keyframe">The keyframe to remove</param>
        public void RemoveKeyframe(BaseKeyframe keyframe)
        {
            BaseKeyframes.Remove(keyframe);
            SortKeyframes();
        }

        /// <summary>
        ///     Returns the flattened index of this property on the layer
        /// </summary>
        /// <returns></returns>
        public int GetFlattenedIndex()
        {
            if (Parent == null)
                return Layer.Properties.ToList().IndexOf(this);

            // Create a flattened list of all properties in their order as defined by the parent/child hierarchy
            var properties = new List<BaseLayerProperty>();
            // Iterate root properties (those with children)
            foreach (var baseLayerProperty in Layer.Properties)
            {
                // First add self, then add all children
                if (baseLayerProperty.Children.Any())
                {
                    properties.Add(baseLayerProperty);
                    properties.AddRange(baseLayerProperty.GetAllChildren());
                }
            }

            return properties.IndexOf(this);
        }

        public override string ToString()
        {
            return $"{nameof(Id)}: {Id}, {nameof(Name)}: {Name}, {nameof(Description)}: {Description}";
        }

        internal void ApplyToEntity()
        {
            var propertyEntity = Layer.LayerEntity.PropertyEntities.FirstOrDefault(p => p.Id == Id);
            if (propertyEntity == null)
            {
                propertyEntity = new PropertyEntity {Id = Id};
                Layer.LayerEntity.PropertyEntities.Add(propertyEntity);
            }

            propertyEntity.ValueType = Type.Name;
            propertyEntity.Value = JsonConvert.SerializeObject(BaseValue);
            propertyEntity.IsUsingKeyframes = IsUsingKeyframes;

            propertyEntity.KeyframeEntities.Clear();
            foreach (var baseKeyframe in BaseKeyframes)
            {
                propertyEntity.KeyframeEntities.Add(new KeyframeEntity
                {
                    Position = baseKeyframe.Position,
                    Value = JsonConvert.SerializeObject(baseKeyframe.BaseValue),
                    EasingFunction = (int) baseKeyframe.EasingFunction
                });
            }
        }

        internal void ApplyToProperty(PropertyEntity propertyEntity)
        {
            BaseValue = DeserializePropertyValue(propertyEntity.Value);
            IsUsingKeyframes = propertyEntity.IsUsingKeyframes;

            BaseKeyframes.Clear();
            foreach (var keyframeEntity in propertyEntity.KeyframeEntities.OrderBy(e => e.Position))
            {
                // Create a strongly typed keyframe or else it cannot be cast later on
                var keyframeType = typeof(Keyframe<>);
                var keyframe = (BaseKeyframe) Activator.CreateInstance(keyframeType.MakeGenericType(Type), Layer, this);
                keyframe.Position = keyframeEntity.Position;
                keyframe.BaseValue = DeserializePropertyValue(keyframeEntity.Value);
                keyframe.EasingFunction = (Easings.Functions) keyframeEntity.EasingFunction;

                BaseKeyframes.Add(keyframe);
            }
        }

        internal void SortKeyframes()
        {
            BaseKeyframes = BaseKeyframes.OrderBy(k => k.Position).ToList();
        }

        internal IEnumerable<BaseLayerProperty> GetAllChildren()
        {
            var children = new List<BaseLayerProperty>();
            children.AddRange(Children);
            foreach (var layerPropertyViewModel in Children)
                children.AddRange(layerPropertyViewModel.GetAllChildren());

            return children;
        }

        private object DeserializePropertyValue(string value)
        {
            if (value == "null")
                return Type.IsValueType ? Activator.CreateInstance(Type) : null;
            return JsonConvert.DeserializeObject(value, Type);
        }

        #region Events

        /// <summary>
        ///     Occurs when this property's value was changed outside regular keyframe updates
        /// </summary>
        public event EventHandler<EventArgs> ValueChanged;

        /// <summary>
        ///     Occurs when this property or any of it's ancestors visibility is changed
        /// </summary>
        public event EventHandler<EventArgs> VisibilityChanged;

        protected virtual void OnValueChanged()
        {
            ValueChanged?.Invoke(this, EventArgs.Empty);
        }

        protected virtual void OnVisibilityChanged()
        {
            VisibilityChanged?.Invoke(this, EventArgs.Empty);
            foreach (var baseLayerProperty in Children)
                baseLayerProperty.OnVisibilityChanged();
        }
        
        #endregion
    }
}