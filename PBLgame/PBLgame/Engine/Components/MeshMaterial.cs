﻿using Microsoft.Xna.Framework.Graphics;

namespace PBLgame.Engine.Components
{
    public class MeshMaterial
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public Texture2D Diffuse { get; set; }
        public Texture2D Normal { get; set; }
        public Texture2D Specular { get; set; }
        public Texture2D Emissive { get; set; }
        public Effect ShaderEffect { get; set; }

        public MeshMaterial(int id, string name, Texture2D diffuse, Texture2D normal, Texture2D specular, Texture2D emissive, Effect shaderEffect)
        {
            Id = id;
            Name = name;
            Diffuse = diffuse;
            Normal = normal;
            Specular = specular;
            Emissive = emissive;
            ShaderEffect = shaderEffect;
        }

        public override string ToString()
        {
            //return string.Format("{0} [{1}]", Name, Id);
            return Name;
        }
    }
}
