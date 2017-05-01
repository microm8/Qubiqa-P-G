using System;
using System.Drawing;
using System.Windows.Forms;
using System.Xml.Serialization;
using Experior.Core.Assemblies;
using System.Collections.Generic;

namespace Experior.Catalog.Qubiqa.Assemblies
{
    public class Gantry : Assembly
    {

        private Core.Parts.Cube horzMover;
        private Core.Parts.Cube vertMover;
        private Core.Parts.Cube loadMover;

        private Frame leftframe;
        private Frame rightframe;
        private GantryInfo info;

        private Core.Timer.Translate xMoverTimer;
        private Core.Timer.Translate yMoverTimer;
        private Core.Timer.Translate zMoverTimer;

        #region Constructors

        public Gantry(GantryInfo info)
            : base(info)
        {
            this.info = info;

            leftframe = new Frame(15, 4, 0.25f);
            rightframe = new Frame(15, 4, 0.25f);

            horzMover = new Core.Parts.Cube(Color.Blue, 6.25f, 0.25f, 0.35f);
            vertMover = new Core.Parts.Cube(Color.Green, 0.25f, leftframe.Height, 0.35f);
            loadMover = new Core.Parts.Cube(Color.Silver, 1f, 0.05f, 1f);

            Add(leftframe);
            Add(rightframe);
            Add(vertMover);
            Add(horzMover);
            Add(loadMover);

            leftframe.LocalPosition = new Microsoft.DirectX.Vector3(0, 0, 3);
            rightframe.LocalPosition = new Microsoft.DirectX.Vector3(0, 0, -3);

            horzMover.LocalYaw = (float)Math.PI / 2;

            xMoverTimer = new Core.Timer.Translate();
            yMoverTimer = new Core.Timer.Translate();
            zMoverTimer = new Core.Timer.Translate();

            xMoverTimer.Add(horzMover);
            xMoverTimer.Add(vertMover);
            xMoverTimer.Add(loadMover);

            yMoverTimer.Add(vertMover);
            yMoverTimer.Add(loadMover);

            zMoverTimer.Add(vertMover);
            zMoverTimer.Add(loadMover);

            Reset();

            Core.Environment.Scene.OnLoaded += Scene_OnLoaded;
        }

        private void Scene_OnLoaded()
        {
            CreateLoads();
        }

        private void CreateLoads()
        {
            foreach (var ap in Core.Routes.ActionPoint.Items.Values)
            {
                if (ap.Name == "AP1" || ap.Name == "AP2" || ap.Name == "AP3" || ap.Name == "AP4" || ap.Name == "AP5" || ap.Name == "AP6" || ap.Name == "AP7" || ap.Name == "AP8")
                {
                    Core.Loads.Load load = Core.Loads.Load.CreateBox(0.6f, 0.4f, 0.6f, Color.OldLace);
                    load.Switch(ap);
                    load.Stop();
                }
            }
        }

        private bool busy = false;
        private Queue<Action> jobs = new Queue<Action>();

        public override void KeyDown(KeyEventArgs e)
        {
            base.KeyDown(e);

            if (e.KeyData == Keys.M)
            {
                if (Core.Loads.Load.Items.Count == 0)
                    return;


                if (busy)
                {
                    jobs.Enqueue(Start);
                    return;
                }

                Start();
            }
        }

        private void Start()
        {
            busy = true;
            //Find a random box
            int idx = Core.Environment.Random.Next(0, Core.Loads.Load.Items.Count - 1);
            Core.Loads.Load load = Core.Loads.Load.Items[idx];

            float x = load.Position.X - loadMover.Position.X;
            float y = load.Position.Y - loadMover.Position.Y + load.Height / 2 + loadMover.Height / 2;
            float z = load.Position.Z - loadMover.Position.Z;

            float time = MaxTime(x, y, z);

            yMoverTimer.Start(new Microsoft.DirectX.Vector3(0, y, 0), time * 2);
            zMoverTimer.Start(new Microsoft.DirectX.Vector3(0, 0, z), time * 2);
            xMoverTimer.Start(new Microsoft.DirectX.Vector3(x, 0, 0), time * 2);

            Core.Timer.Action(() => Pickup(load), time * 2 + 1);
        }

        private float MaxTime(float x, float y, float z)
        {
            if (Math.Abs(x) > Math.Abs(z) && Math.Abs(x) > Math.Abs(y))
                return Math.Abs(x);

            if (Math.Abs(y) > Math.Abs(z) && Math.Abs(y) > Math.Abs(x))
                return Math.Abs(y);

            return Math.Abs(z);
        }

        private float MaxTime(float x, float z)
        {
            if (Math.Abs(x) > Math.Abs(z))
                return Math.Abs(x);

            return Math.Abs(z);
        }


        private void Lifted(Core.Loads.Load load)
        {
            //Find an empty ActionPoint
            Core.Routes.ActionPoint ap;
            while (true)
            {
                int idx = Core.Environment.Random.Next(1, Core.Routes.ActionPoint.Items.Values.Count - 1);
                ap = Core.Routes.ActionPoint.Items["AP" + idx];

                if (ap.ActiveLoad == null)
                    break;
            }

            float x = ap.Position.X - loadMover.Position.X;
            float z = ap.Position.Z - loadMover.Position.Z;

            float time = MaxTime(x, z);

            zMoverTimer.Start(new Microsoft.DirectX.Vector3(0, 0, z), time * 2);
            xMoverTimer.Start(new Microsoft.DirectX.Vector3(x, 0, 0), time * 2);

            Core.Timer.Action(() => Arrived(load,ap), time * 2 + 1);

        }

        private void Arrived(Core.Loads.Load load, Core.Routes.ActionPoint ap)
        {
            float y = ap.Position.Y - loadMover.Position.Y + load.Height + loadMover.Height / 2;
            
            yMoverTimer.Start(new Microsoft.DirectX.Vector3(0, y, 0), Math.Abs(y) * 2);

            Core.Timer.Action(() => Drop(load, ap), Math.Abs(y) * 2 + 1);
        }

        private void Drop(Core.Loads.Load load, Core.Routes.ActionPoint ap)
        {
            float y = vertMover.Position.Y - leftframe.Height + horzMover.Height / 2;

            loadMover.UnAttach();
            load.Switch(ap);
            load.Stop();

            yMoverTimer.Start(new Microsoft.DirectX.Vector3(0, -y, 0), Math.Abs(y) * 2);

            Core.Timer.Action(() => Completed(load), Math.Abs(y) * 2 + 1);
        }

        private void Completed(Core.Loads.Load load)
        {
            busy = false;

            if (jobs.Count > 0)
            {
                Action action = jobs.Dequeue();
                action.Invoke();
            }
        }

        private void Pickup(Core.Loads.Load load)
        {
            //Go up
            load.Release();
            loadMover.Attach(load);
            float y = vertMover.Position.Y - leftframe.Height + horzMover.Height / 2;
            yMoverTimer.Start(new Microsoft.DirectX.Vector3(0, -y, 0), Math.Abs(y) * 2);

            Core.Timer.Action(() => Lifted(load), Math.Abs(y) * 2 + 1);
        }

        public override void Reset()
        {
            base.Reset();

            busy = false;

            jobs.Clear();

            xMoverTimer.Reset();
            yMoverTimer.Reset();
            zMoverTimer.Reset();

            horzMover.LocalPosition = new Microsoft.DirectX.Vector3(0, leftframe.Height + horzMover.Height / 2, 0);
            vertMover.LocalPosition = horzMover.LocalPosition + new Microsoft.DirectX.Vector3(vertMover.Length/2+horzMover.Width/2, 0, 0);
            loadMover.LocalPosition = vertMover.LocalPosition + new Microsoft.DirectX.Vector3(0, -vertMover.Height/2-loadMover.Height/2, 0);

            CreateLoads();
        }

        public override void Render()
        {
            base.Render();

            xMoverTimer.Render();
            yMoverTimer.Render();
            zMoverTimer.Render();
        }

        public override void Inserted()
        {
            this.Position = new Microsoft.DirectX.Vector3(Position.X, 0, Position.Z);

        }

        #endregion

        #region Properties

        public override string Category
        {
            get { return "Assembly"; }
        }

        public override Image Image
        {
            get { return Common.Icons.Get("MyAssembly"); }
        }

        #endregion
    }

    [Serializable, XmlInclude(typeof(GantryInfo)), XmlType(TypeName = "Experior.Catalog.Qubiqa.Assemblies.GantryInfo")]
    public class GantryInfo : Experior.Core.Assemblies.AssemblyInfo
    {
        #region Fields

        private readonly static GantryInfo properties = new GantryInfo();

        #endregion

        #region Properties

        public static object Properties
        {
            get
            {
                properties.color = Experior.Core.Environment.Scene.DefaultColor;
                return properties;
            }
        }

        #endregion
    }
}