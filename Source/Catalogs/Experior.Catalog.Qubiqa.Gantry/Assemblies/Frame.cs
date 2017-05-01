using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Xml.Serialization;

using Experior.Core.Mathematics;
using Experior.Core.Parts;
using Experior.Core.Properties;

using Microsoft.DirectX;

namespace Experior.Catalog.Qubiqa.Assemblies
{
    [TypeConverter(typeof(ObjectConverter))]
    public class Frame : Core.Assemblies.Assembly
    {
        #region Fields

      //  private const float framesize = 0.03f;

        private Core.Parts.Cube left;
        private Core.Parts.CurveArea leftcorner;
        private Core.Parts.Cube right;
        private Core.Parts.CurveArea rightcorner;
        private bool roundconners = true;
        private Core.Parts.Cube top;

        #endregion

        #region Constructors

        public Frame(float length, float height, float width)
            : base(new Core.Assemblies.AssemblyInfo())
        {
            Info.length = length;
            Info.width = width;
            Info.height = height;

            top = new Core.Parts.Cube(Color.Silver, Length, Info.width, Info.width);
            left = new Core.Parts.Cube(Color.Silver, Info.width, Height, Info.width);
            right = new Core.Parts.Cube(Color.Silver, Info.width, Height, Info.width);

            Add(top);
            Add(left);
            Add(right);

            Refresh();
        }

        #endregion

        #region Properties


        /// <summary>
        /// Gets the category.
        /// </summary>
        /// <value>The category.</value>
        public override string Category
        {
            get
            {
                return "Frame";
            }
        }


        [Category("Size")]
        [PropertyOrder(1)]
        [TypeConverter(typeof(Core.Properties.TypeConverter.FloatMeterToMillimeter))]
        [Description(Experior.Core.Properties.TypeConverter.FloatMeterToMillimeter.Description)]
        public float Height
        {
            get
            {
                return Info.height;
            }
            set
            {
                if (value <= 0)
                    return;

                Info.height = value;
                InvokeRefresh();
            }
        }

        /// <summary>
        /// Gets the image.
        /// </summary>
        /// <value>The image.</value>
        public override Image Image
        {
            get
            {
                return null;
            }
        }

        [Category("Size")]
        [PropertyOrder(0)]
        [TypeConverter(typeof(Core.Properties.TypeConverter.FloatMeterToMillimeter))]
        [Description(Core.Properties.TypeConverter.FloatMeterToMillimeter.Description)]
        public float Length
        {
            get
            {
                return Info.length;
            }
            set
            {
                if (value <= 0)
                    return;

                Info.length = value;
                InvokeRefresh();
            }
        }

        public bool RoundCorners
        {
            get
            {
                return roundconners;
            }
            set
            {
                roundconners = value;
                InvokeRefresh();
            }
        }

        internal Cube Left
        {
            get
            {
                return left;
            }
        }

        internal CurveArea LeftCorner
        {
            get
            {
                return leftcorner;
            }
        }

        internal Cube Right
        {
            get
            {
                return right;
            }
        }

        internal CurveArea RightCorner
        {
            get
            {
                return rightcorner;
            }
        }

        internal Cube Top
        {
            get
            {
                return top;
            }
        }

        #endregion

        #region Methods


        public override void Inserted()
        {
            base.Inserted();
            Position -= new Vector3(0, Position.Y - Core.Environment.Scene.Section.Position.Y, 0);
        }

        public override void Refresh()
        {
            if (RoundCorners && leftcorner == null)
            {
                leftcorner = new Core.Parts.CurveArea(Color.Silver, Info.width, Info.width, 0.1f, 90, Core.Environment.Revolution.Clockwise);
                rightcorner = new Core.Parts.CurveArea(Color.Silver, Info.width, Info.width, 0.1f, 90, Core.Environment.Revolution.Clockwise);

                Add(leftcorner);
                Add(rightcorner);
            }

            if (!RoundCorners && leftcorner != null)
            {
                Remove(leftcorner);
                Remove(rightcorner);

                leftcorner.Dispose();
                rightcorner.Dispose();

                leftcorner = null;
                rightcorner = null;
            }

            if (LeftCorner != null)
            {
                left.Height = Height - LeftCorner.Radius;
                right.Height = Height - LeftCorner.Radius;
                top.Length = Length - LeftCorner.Radius * 2;
            }
            else
            {
                left.Height = Height;
                right.Height = Height;
                top.Length = Length + right.Length / 2 + right.Length / 2;
            }


            if (LeftCorner != null)
                left.LocalPosition = new Vector3(-Length / 2, Height / 2 - leftcorner.Radius / 2 - top.Height / 2, 0);
            else
                left.LocalPosition = new Vector3(-Length / 2, Height / 2 - top.Height / 2, 0);

            right.LocalPosition = left.LocalPosition + new Vector3(Length, 0, 0);

            top.LocalPosition = new Vector3(0, Height - top.Height / 2, 0);

            if (LeftCorner != null)
                leftcorner.LocalPosition = top.LocalPosition + new Vector3(-Length / 2 + leftcorner.Radius, -leftcorner.Radius, 0);

            if (RightCorner != null)
                RightCorner.LocalPosition = top.LocalPosition + new Vector3(Length / 2 - rightcorner.Radius, -rightcorner.Radius, 0);

            if (LeftCorner != null)
            {
                LeftCorner.LocalRoll = (float)Math.PI / 2;
                LeftCorner.LocalYaw = -(float)Math.PI / 2;
            }

            if (RightCorner != null)
            {
                rightcorner.LocalRoll = (float)Math.PI / 2;
                rightcorner.LocalYaw = (float)Math.PI / 2;
            }

            int nohorz = (int)(Length / 0.15f);
            int novert = (int)(Height / 0.2f);

        }

        #endregion
    }
}