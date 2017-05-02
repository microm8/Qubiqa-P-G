using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Xml.Serialization;
using Experior.Core.Assemblies;
using Experior.Core.Loads;
using Experior.Core.Motors;
using Experior.Core.Parts;
using Experior.Core.Routes;
using Experior.Core.TransportSections;
using Microsoft.DirectX;
using Environment = Experior.Core.Environment;

namespace Experior.Catalog.Qubiqa.TransferCar.Assemblies
{
    public class TransferCar : Assembly
    {
        public enum TransferCarSide
        {
            Left,
            Right
        }

        private readonly StraightTransportSection transport;
        private readonly StraightTransportSection car1Transport;
        private readonly Cube rail1, rail2;
        private readonly Load car;
        private readonly ActionPoint destination;
        private readonly ActionPoint car1OnBoard;
        private readonly ActionPoint car1Leaving;

        private TransferCarJob currentJob;
        private readonly List<TransferCarJob> jobQueue = new List<TransferCarJob>();
        private bool finishedmoving;
        private bool car1Done;

        private float transferCarSpeed = 5;
        private float conveyorSpeed = 1;
        //Height was modified
        private float carHeight = 0.850f;
        private Color carColor = Color.FromArgb(100, 149, 237);

        public TransferCar(TransferCarInfo info) : base(info)
        {
            transport = new StraightTransportSection(Color.Blue, info.length, 0, 0.1f);
            Add(transport);
            transport.LocalYaw = (float)Math.PI;
            transport.LocalPosition = new Vector3(transport.Length / 2, 0, 0);
            transport.Visible = false;

            destination = new ActionPoint();
            transport.Route.InsertActionPoint(destination);
            destination.Distance = transport.Length / 2;
            destination.StopMode = ActionPoint.StoppingMode.Stop;
            destination.OnEnter += DestinationOnEnter;

            car1Transport = new StraightTransportSection(carColor, 1.8f, carHeight, 1.0f);
            Add(car1Transport);
            car1Transport.LocalYaw = (float)Math.PI / 2;
            car1Transport.Route.Motor.Speed = conveyorSpeed;


            //Z-Position of the rails were modified
            rail1 = new Cube(Color.Gray, transport.Length, 0.1f, 0.1f);
            Add(rail1);
            rail1.LocalPosition = new Vector3(transport.Length / 2, 0, 0.540f);
            rail2 = new Cube(Color.Gray, transport.Length, 0.1f, 0.1f);
            Add(rail2);
            rail2.LocalPosition = new Vector3(transport.Length / 2, 0, -0.540f);

            car = Load.CreateBox(0.1f, 0.1f, 0.1f, Color.Red);
            transport.Route.Add(car);
            car.Deletable = false;
            car.Embedded = true;
            car.Stop();
            car.OnPositionChanged += Car_OnPositionChanged;
            car.Distance = destination.Distance;
            car.Visible = false;

            car1OnBoard = car1Transport.Route.InsertActionPoint(car1Transport.Length / 2);
            car1OnBoard.OnEnter += Car1OnBoard_OnEnter;

            car1Leaving = car1Transport.Route.InsertActionPoint(car1Transport.Length);
            car1Leaving.Edge = ActionPoint.Edges.Trailing;
            car1Leaving.OnEnter += Car1Leaving_OnEnter;
        }

        public bool Running => currentJob != null || jobQueue.Any();
        public bool Car1Occupied => car1Transport.Route.Loads.Any();
        public float TransferCarWidth => car1Transport.Width;

        public void AddJob(TransferCarJob job)
        {
            jobQueue.Add(job);
            if (currentJob == null)
                NextJob();
        }

        private void Car1Leaving_OnEnter(ActionPoint sender, Load load)
        {
            if (currentJob?.JobType != TransferCarJob.JobTypes.PickDrop)
                return;

            car1Done = true;

            if (car1Done)
            {
                currentJob = null;
                NextJob();
            }
        }

        private void Car1OnBoard_OnEnter(ActionPoint sender, Load load)
        {
            sender.Parent.Motor.Stop();
            if (currentJob?.JobType != TransferCarJob.JobTypes.PickDrop)
                return;

            car1Done = true;

            if (car1Done)
            {
                currentJob = null;
                NextJob();
            }
        }

        public override void Reset()
        {
            currentJob = null;
            finishedmoving = false;
            jobQueue.Clear();
            car1Done = false;
            destination.Distance = transport.Length / 2;
            car.Distance = destination.Distance;
            car.Stop();
            base.Reset();
        }

        private void Car_OnPositionChanged(Load load, Vector3 position)
        {
            car1Transport.LocalPosition = new Vector3(load.Distance, carHeight / 2, 0);
        }

        private void DestinationOnEnter(ActionPoint sender, Load load)
        {
            if (currentJob?.JobType != TransferCarJob.JobTypes.Goto)
                return;

            finishedmoving = true;

            currentJob = null;
            NextJob();
        }

        private void GotoJob()
        {
            car1Transport.Route.NextRoute = null;
            car1Transport.Route.PreviousRoute = null;
            GotoJobReleasingMoveLoad();

            if (finishedmoving)
            {
                currentJob = null;
                NextJob();
            }
        }

        private void SetTransportLocalYaw(StraightTransportSection carTransport, TransferCarSide side, Motor.Directions direction, TransferCarJob.PickDropInfo.PickDropTypes jobType)
        {
            if (jobType == TransferCarJob.PickDropInfo.PickDropTypes.Pick)
            {
                if (direction == Motor.Directions.Forward)
                {
                    if (side == TransferCarSide.Right)
                    {
                        carTransport.LocalYaw = (float)Math.PI / 2;
                    }
                    else
                    {
                        carTransport.LocalYaw = -(float)Math.PI / 2;
                    }
                }
                else
                {
                    if (side == TransferCarSide.Right)
                    {
                        carTransport.LocalYaw = -(float)Math.PI / 2;
                    }
                    else
                    {
                        carTransport.LocalYaw = (float)Math.PI / 2;
                    }
                }
            }
            else
            {
                if (direction == Motor.Directions.Forward)
                {
                    if (side == TransferCarSide.Left)
                    {
                        carTransport.LocalYaw = (float)Math.PI / 2;
                    }
                    else
                    {
                        carTransport.LocalYaw = -(float)Math.PI / 2;
                    }
                }
                else
                {
                    if (side == TransferCarSide.Left)
                    {
                        carTransport.LocalYaw = -(float)Math.PI / 2;
                    }
                    else
                    {
                        carTransport.LocalYaw = (float)Math.PI / 2;
                    }
                }
            }
        }

        private void SetTransportMotorAndRoute(StraightTransportSection carTransport, Motor.Directions direction, Route next, TransferCarJob.PickDropInfo.PickDropTypes jobType, ActionPoint leaving)
        {
            if (direction == Motor.Directions.Forward)
            {
                if (carTransport.Route.Motor.Direction != direction)
                {
                    carTransport.Route.Motor.SwitchDirection();
                }
                if (next.Motor.Direction != direction)
                {
                    next.Motor.SwitchDirection();
                }
                if (jobType == TransferCarJob.PickDropInfo.PickDropTypes.Drop)
                {
                    carTransport.Route.NextRoute = next;
                }
                else
                {
                    next.NextRoute = carTransport.Route;
                }
                leaving.Distance = carTransport.Length;
                carTransport.Route.Motor.Start();
            }
            else
            {
                if (carTransport.Route.Motor.Direction != direction)
                {
                    carTransport.Route.Motor.SwitchDirection();
                }
                if (next.Motor.Direction != direction)
                {
                    next.Motor.SwitchDirection();
                }
                if (jobType == TransferCarJob.PickDropInfo.PickDropTypes.Drop)
                {
                    carTransport.Route.PreviousRoute = next;
                }
                else
                {
                    next.PreviousRoute = carTransport.Route;
                }
                leaving.Distance = 0;
                carTransport.Route.Motor.Start();
            }
        }

        private void PickJob()
        {
            if (currentJob == null)
                return;

            foreach (var pick in currentJob.PickDropInfos.Where(p => p.JobType == TransferCarJob.PickDropInfo.PickDropTypes.Pick))
            {
                if (!pick.ActionPoint.Active)
                {
                    Log.Write($"Error when picking: No load at {pick.ActionPoint.Name}");
                    jobQueue.Clear();
                    currentJob = null;
                    return;
                }

                car1Done = false;
                SetTransportLocalYaw(car1Transport, pick.TransferSide, pick.DestinationMotorDirection, pick.JobType);
                SetTransportMotorAndRoute(car1Transport, pick.DestinationMotorDirection, pick.ActionPoint.Parent, pick.JobType, car1Leaving);

                pick.ActionPoint.Release();
            }
        }

        private void DropJob()
        {
            if (currentJob == null)
                return;

            foreach (var drop in currentJob.PickDropInfos.Where(d => d.JobType == TransferCarJob.PickDropInfo.PickDropTypes.Drop))
            {
                car1Done = false;
                SetTransportLocalYaw(car1Transport, drop.TransferSide, drop.DestinationMotorDirection, drop.JobType);
                SetTransportMotorAndRoute(car1Transport, drop.DestinationMotorDirection, drop.ActionPoint.Parent, drop.JobType, car1Leaving);
            }
        }

        private void NextJob()
        {
            if (currentJob != null)
                return;

            car1Done = true;

            if (jobQueue.Count > 0)
            {
                currentJob = jobQueue[0];
                jobQueue.RemoveAt(0);
                if (Environment.Debug.Level == Environment.Debug.Levels.Level1)
                    Log.Write(Name + ": Starting job: " + currentJob);

                switch (currentJob.JobType)
                {
                    case TransferCarJob.JobTypes.Goto:
                        GotoJob();
                        break;
                    case TransferCarJob.JobTypes.PickDrop:
                        PickJob();
                        DropJob();
                        break;
                }
            }
            else
            {
                if (Environment.Debug.Level == Environment.Debug.Levels.Level1)
                    Log.Write(Name + ": Finished job queue.");
            }
        }

        private void GotoJobReleasingMoveLoad()
        {
            if (currentJob.DestinationLength < 0.001f)
            {
                currentJob.DestinationLength = 0.001f;
            }

            if (currentJob.DestinationLength > transport.Route.Length - 0.001f)
            {
                currentJob.DestinationLength = transport.Route.Length - 0.001f;
            }

            destination.Distance = car.Distance;
            destination.Distance = currentJob.DestinationLength;

            if (car.Distance < currentJob.DestinationLength)
                transport.Route.Motor.Speed = transferCarSpeed;
            else
                transport.Route.Motor.Speed = -transferCarSpeed;

            if (Math.Abs(car.Distance - currentJob.DestinationLength) >= 0.05f)
                finishedmoving = false;
            else
                finishedmoving = true;

            if (!finishedmoving)
                car.Release();
        }


        public override void Dispose()
        {
            car1OnBoard.OnEnter -= Car1OnBoard_OnEnter;
            car1Leaving.OnEnter -= Car1Leaving_OnEnter;
            destination.OnEnter -= DestinationOnEnter;
            car.OnPositionChanged -= Car_OnPositionChanged;
            car.Deletable = false;
            car.Dispose();
            base.Dispose();
        }

        public override string Category { get; } = "Transfer car";
        public override Image Image { get; } = null;

        [DisplayName("Length")]
        [Category("Configuration")]
        [TypeConverter(typeof(Core.Properties.TypeConverter.FloatMeterToMillimeter))]
        public float Length
        {
            get { return Info.length; }
            set
            {
                if (value < 1)
                    return;

                Info.length = value;

                Environment.Invoke(Rebuild);
            }
        }

        private void Rebuild()
        {
            transport.Length = Info.length;
            //Z-Position of the rails were modified
            rail1.Length = Info.length;
            rail1.LocalPosition = new Vector3(transport.Length / 2, 0, 0.540f);
            rail2.Length = Info.length;
            rail2.LocalPosition = new Vector3(transport.Length / 2, 0, -0.540f);
            Reset();
            CalculateBoundaries();
        }
    }

    [Serializable]
    [XmlInclude(typeof(TransferCarInfo))]
    [XmlType(TypeName = "Experior.Catalog.DanishCrown.TransferCarInfo")]
    public class TransferCarInfo : AssemblyInfo
    {

    }

    public class TransferCarJob
    {
        public float DestinationLength { get; set; }
        public JobTypes JobType { get; set; }
        public List<PickDropInfo> PickDropInfos = new List<PickDropInfo>();

        public enum JobTypes
        {
            Goto,
            PickDrop
        }

        public class PickDropInfo
        {
            public Motor.Directions DestinationMotorDirection { get; set; }
            public ActionPoint ActionPoint { get; set; }
            public TransferCar.TransferCarSide TransferSide { get; set; }
            public PickDropTypes JobType { get; set; }

            public enum PickDropTypes
            {
                Pick,
                Drop
            }
        }
    }
}