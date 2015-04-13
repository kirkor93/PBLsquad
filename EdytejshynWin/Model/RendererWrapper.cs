using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using Microsoft.Xna.Framework.Graphics;
using PBLgame.Engine.Components;

namespace Edytejshyn.Model
{
    public class RendererWrapper
    {

        public class EffectTypeConverter : TypeConverter
        {
            public override bool GetStandardValuesSupported(ITypeDescriptorContext context) { return true; }
            public override bool GetStandardValuesExclusive(ITypeDescriptorContext context) { return true; }

            public override StandardValuesCollection GetStandardValues(ITypeDescriptorContext context)
            {
                var collection = new Collection<Effect>(Program.UglyStaticLogic.ResourceManager.ShaderEffects);
                return new StandardValuesCollection(collection);
            }

            public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
            {
                return sourceType == typeof(string) || base.CanConvertFrom(context, sourceType);
            }

            public override object ConvertFrom(ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object value)
            {
                string name = value as string;
                if (name != null)
                {
                    foreach (Effect effect in Program.UglyStaticLogic.ResourceManager.ShaderEffects)
                    {
                        if (name == effect.ToString()) return effect;
                    }
                }
                return base.ConvertFrom(context, culture, value);
            }
        }
        public class MeshTypeConverter : TypeConverter
        {
            public override bool GetStandardValuesSupported(ITypeDescriptorContext context) { return true; }
            public override bool GetStandardValuesExclusive(ITypeDescriptorContext context) { return true; }

            public override StandardValuesCollection GetStandardValues(ITypeDescriptorContext context)
            {
                var collection = new Collection<Mesh>(Program.UglyStaticLogic.ResourceManager.Meshes);
                return new StandardValuesCollection(collection);
            }

            public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
            {
                return sourceType == typeof(string) || base.CanConvertFrom(context, sourceType);
            }

            public override object ConvertFrom(ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object value)
            {
                string name = value as string;
                return (name != null)
                    ? Program.UglyStaticLogic.ResourceManager.GetMesh(name)
                    : base.ConvertFrom(context, culture, value);
            }
        }
        public class MaterialTypeConverter : TypeConverter
        {
            public override bool GetStandardValuesSupported(ITypeDescriptorContext context) { return true; }
            public override bool GetStandardValuesExclusive(ITypeDescriptorContext context) { return true; }

            public override StandardValuesCollection GetStandardValues(ITypeDescriptorContext context)
            {
                var collection = new Collection<MeshMaterial>(Program.UglyStaticLogic.ResourceManager.Materials);
                return new StandardValuesCollection(collection);
            }

            public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
            {
                return sourceType == typeof(string) || base.CanConvertFrom(context, sourceType);
            }

            public override object ConvertFrom(ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object value)
            {
                string name = value as string;
                if (name != null)
                {
                    int id = int.Parse(name.Split(':')[0]);
                    return Program.UglyStaticLogic.ResourceManager.GetMaterial(id);
                }
                return base.ConvertFrom(context, culture, value);
            }
        }
        #region Variables

        public readonly GameObjectWrapper Parent;

        #endregion

        #region Properties
        [Browsable(false)]
        public Renderer WrappedRenderer { get; private set; }

        [TypeConverter(typeof(MeshTypeConverter))]
        public Mesh Mesh
        {
            get { return WrappedRenderer.MyMesh; }
            set { Parent.FireSetter(x => WrappedRenderer.MyMesh = x, WrappedRenderer.MyMesh, value); }
        }

        [TypeConverter(typeof(MaterialTypeConverter))]
        public MeshMaterial Material
        {
            get { return WrappedRenderer.Material; }
            set { Parent.FireSetter(x => WrappedRenderer.Material = x, WrappedRenderer.Material, value); }
        }

        [TypeConverter(typeof (EffectTypeConverter))]
        [DisplayName("!TEMPORARY! Shader effect")]
        public Effect ShaderEffect
        {
            // TODO should be only in material
            get { return WrappedRenderer.MyEffect; }
            set { Parent.FireSetter(x => WrappedRenderer.MyEffect = x, WrappedRenderer.MyEffect, value); }
        }

        #endregion

        #region Methods
        public RendererWrapper(GameObjectWrapper parent, Renderer renderer)
        {
            Parent = parent;
            WrappedRenderer = renderer;
        }

        public void Draw(IDrawerStrategy drawerStrategy)
        {
            drawerStrategy.Draw(Parent);
        }

        public override string ToString()
        {
            return Mesh.ToString();
        }

        #endregion
    }
}