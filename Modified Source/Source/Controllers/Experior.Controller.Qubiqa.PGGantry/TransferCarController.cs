using System.Collections.Generic;
using System.Linq;
using Experior.Catalog.Qubiqa.TransferCar.Assemblies;
using Experior.Core;
using Experior.Core.Motors;
using Experior.Core.Routes;
using Microsoft.DirectX;

namespace Experior.Controller.Qubiqa.PGGantry
{
    public class TransferCarController
    {
        private readonly HashSet<ActionPoint> tCarNodes = new HashSet<ActionPoint>();
        private readonly TransferCar transferCar;
        //private ActionPoint tcar1In;
        //private ActionPoint tcar1Out;
        //private ActionPoint ap1;
        //private ActionPoint ap2;

        private readonly Timer timer = new Timer(0.5f);
        private bool ready = true;

        public TransferCarController(TransferCar transferCar)
        {
            this.transferCar = transferCar;
            timer.AutoReset = true;
            timer.OnElapsed += Timer_OnElapsed;
            timer.Start();
            Environment.Scene.OnStarting += Scene_OnStarting;
            Environment.Scene.OnLoaded += Initialize;
        }

        private void Initialize()
        {
            //CreateTCarNode(tcar1In = ActionPoint.Get("TCAR1IN"));
            //CreateTCarNode(tcar1Out = ActionPoint.Get("TCAR1OUT"));
            //CreateTCarNode(ap1 = ActionPoint.Get("AP1"));
            //CreateTCarNode(ap2 = ActionPoint.Get("AP2"));
        }

        private void CreateTCarNode(ActionPoint ap)
        {
            if (ap == null)
            {
                Log.Write("Error: not all T car nodes found.");
                ready = false;
                return;
            }

            var distance = 0f;
            var side = TransferSide(ap);
            var dir = ap.Parent.End - ap.Parent.Start;
            var mydir = new Vector3(0, 0, 1);
            mydir.TransformCoordinate(transferCar.Orientation);
            var dot = Vector3.Dot(dir, mydir);
            if (side == TransferCar.TransferCarSide.Left && dot < 0)
            {
                distance = ap.Parent.Length;
            }
            if (side == TransferCar.TransferCarSide.Right && dot > 0)
            {
                distance = ap.Parent.Length;
            }

            var tCarNode = ap.Parent.InsertActionPoint(distance);
            tCarNode.Name = "T CAR";
            tCarNode.Routing = true;
            tCarNodes.Add(tCarNode);
        }

        public void Dispose()
        {
            timer.OnElapsed -= Timer_OnElapsed;
            Environment.Scene.OnStarting -= Scene_OnStarting;
            Environment.Scene.OnLoaded -= Initialize;
            timer.Dispose();
            foreach (var node in tCarNodes)
            {
                node.Dispose();
            }
            tCarNodes.Clear();
        }

        private void Scene_OnStarting()
        {
            timer.Start();
        }

        private void Timer_OnElapsed(Timer sender)
        {
            if (!ready)
                return;

            CheckPriorities();
        }

        private float Distance(ActionPoint destination)
        {
            Vector3 dir = new Vector3(1, 0, 0);
            dir.TransformCoordinate(transferCar.Orientation);

            var ap = destination.Position - transferCar.Position;
            var d = Vector3.Dot(dir, ap);

            return d;
        }

        private Motor.Directions DestinationMotorDirection(ActionPoint destination, TransferCarJob.PickDropInfo.PickDropTypes type)
        {
            var result = Motor.Directions.Forward;
            var side = TransferSide(destination);
            var dir = destination.Parent.End - destination.Parent.Start;
            var mydir = new Vector3(0, 0, 1);
            mydir.TransformCoordinate(transferCar.Orientation);
            var dot = Vector3.Dot(dir, mydir);
            if (side == TransferCar.TransferCarSide.Left)
            {
                if (dot < 0)
                {
                    result = type == TransferCarJob.PickDropInfo.PickDropTypes.Pick ? Motor.Directions.Forward : Motor.Directions.Backward;
                }
                else
                {
                    result = type == TransferCarJob.PickDropInfo.PickDropTypes.Pick ? Motor.Directions.Backward : Motor.Directions.Forward;
                }
            }
            if (side == TransferCar.TransferCarSide.Right)
            {
                if (dot > 0)
                {
                    result = type == TransferCarJob.PickDropInfo.PickDropTypes.Drop ? Motor.Directions.Backward : Motor.Directions.Forward;
                }
                else
                {
                    result = type == TransferCarJob.PickDropInfo.PickDropTypes.Drop ? Motor.Directions.Forward : Motor.Directions.Backward;
                }
            }

            return result;
        }

        private TransferCar.TransferCarSide TransferSide(ActionPoint destination)
        {
            Vector3 dir = new Vector3(0, 0, 1);
            dir.TransformCoordinate(transferCar.Orientation);

            var ap = destination.Position - transferCar.Position;
            var d = Vector3.Dot(dir, ap);

            if (d > 0)
            {
                return TransferCar.TransferCarSide.Left;
            }

            return TransferCar.TransferCarSide.Right;
        }
    
        private void DebugLog(string message)
        {
            if (Environment.Debug.Level != Environment.Debug.Levels.Disabled)
                Environment.Log.Write(message);
        }

        private void DebugLogExecuting(string priority)
        {
            DebugLog($"Transfer car executing priority {priority}");
        }

        private void CheckPriorities()
        {
            if (transferCar.Running)
                return;

            //if (EmptyAndFull(ap1, tcar1In))
            //{
            //    //Priority 1
            //    PickupAndDrop(tcar1In, ap1);
            //    DebugLogExecuting("1");
            //    return;
            //}
            //if (EmptyAndFull(ap2, tcar1In))
            //{
            //    //Priority 2
            //    PickupAndDrop(tcar1In, ap2);
            //    DebugLogExecuting("2");
            //    return;
            //}
            //if (EmptyAndFull(tcar1Out, ap2))
            //{
            //    //Priority 3
            //    PickupAndDrop(ap2, tcar1Out);
            //    DebugLogExecuting("3");
            //    return;
            //}
        }

        public void PickupAndDrop(ActionPoint pickPoint, ActionPoint dropPoint)
        {
            //Goto pick
            var job = new TransferCarJob
            {
                JobType = TransferCarJob.JobTypes.Goto,
                DestinationLength = Distance(pickPoint)
            };
            transferCar.AddJob(job);

            //Pick pallet
            job = new TransferCarJob { JobType = TransferCarJob.JobTypes.PickDrop };
            var pick = new TransferCarJob.PickDropInfo();
            job.PickDropInfos.Add(pick);
            pick.TransferSide = TransferSide(pickPoint);
            pick.ActionPoint = pickPoint;
            pick.JobType = TransferCarJob.PickDropInfo.PickDropTypes.Pick;
            pick.DestinationMotorDirection = DestinationMotorDirection(pickPoint, pick.JobType);
            transferCar.AddJob(job);

            //Goto drop
            job = new TransferCarJob
            {
                JobType = TransferCarJob.JobTypes.Goto,
                DestinationLength = Distance(dropPoint)
            };
            transferCar.AddJob(job);

            //Drop pallet
            job = new TransferCarJob { JobType = TransferCarJob.JobTypes.PickDrop };
            var drop = new TransferCarJob.PickDropInfo();
            job.PickDropInfos.Add(drop);
            drop.TransferSide = TransferSide(dropPoint);
            drop.ActionPoint = dropPoint;
            drop.JobType = TransferCarJob.PickDropInfo.PickDropTypes.Drop;
            drop.DestinationMotorDirection = DestinationMotorDirection(dropPoint, drop.JobType);
            transferCar.AddJob(job);
        }

        private bool EmptyAndFull(ActionPoint empty, ActionPoint full)
        {
            if (full.Active && !empty.Parent.Loads.Any())
                return true;

            return false;
        }

        public void Reset()
        {
            
        }
    }
}