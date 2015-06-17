﻿using System;
using System.Globalization;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PBLgame.Engine.Singleton;

namespace PBLgame.Engine.GUI
{
    public class IntroScene : GUIObject
    {
        #region Variables
        private TimeSpan _length;
        private Texture2D _fontBg;
        #endregion
        #region Properties
        public TimeSpan Length
        {
            get { return _length; }
            set { _length = value; }
        }


        #endregion
        #region Methods

        public IntroScene()
        {
            _fontBg = ResourceManager.Instance.GetTexture(@"Textures\GUI\Font_background");

        }

        public override void Draw(SpriteBatch batch)
        {
            if (!Enabled)
            {
                return;
            }
            batch.Draw(Texture, _boundries, Color.White);
            if (Text != null)
            {
                Vector2 stringSize = Text.Font.MeasureString(Text.Text);
                Vector2 position = new Vector2(_boundries.Center.X, _boundries.Bottom);
                position.X -= (stringSize.X / 2.0f);
                position.Y -= (stringSize.Y + _boundries.Height * 0.05f);

                Rectangle rect = new Rectangle((int) position.X - 10, (int) position.Y - 10, (int) stringSize.X + 20,
                    (int) stringSize.Y + 20);
                batch.Draw(_fontBg, rect, Color.White);
                batch.DrawString(Text.Font, Text.Text, position, Color.White);
            }
        }

        #region XmlSerialization

        public override void ReadXml(XmlReader reader)
        {
            CultureInfo culture = CultureInfo.InvariantCulture;

            base.ReadXml(reader);
            Length = new TimeSpan(0, 0, Convert.ToInt32(reader.GetAttribute("Length"), culture));
            reader.Read();
        }

        public override void WriteXml(XmlWriter writer)
        {
            CultureInfo culture = CultureInfo.InvariantCulture;

            base.WriteXml(writer);
            writer.WriteStartElement("IntroScene");
            writer.WriteAttributeString("Length", Length.Seconds.ToString("G", culture));
            writer.WriteEndElement();
        }

        #endregion
        #endregion
    }
}