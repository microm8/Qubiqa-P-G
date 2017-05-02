using System.Windows.Forms;
using Experior.Catalog.Qubiqa.TransferCar.Assemblies;
using Experior.Core.Loads;
using Experior.Core.Routes;
using System;
using System.Collections.Generic;

namespace Experior.Controller.Qubiqa.PGGantry
{
    public class Controller : Core.Controller
    {
        private TransferCarController transferCarController1;
        private TransferCarController transferCarController2;

        private Dictionary<ActionPoint, bool> aisle1Locations;
        private Dictionary<ActionPoint, bool> aisle2Locations;

        public Controller()
            : base("Qubiqa.PGGantry")
        {
            var tcar1 = Experior.Core.Assemblies.Assembly.Items["Transfer car 1"] as TransferCar;
            if (tcar1 != null)
                transferCarController1 = new TransferCarController(tcar1);

            var tcar2 = Experior.Core.Assemblies.Assembly.Items["Transfer car 2"] as TransferCar;
            if (tcar2 != null)
                transferCarController2 = new TransferCarController(tcar2);

            Experior.Catalog.Logistic.Track.DiverterMerger InFeed1DMUnit = Experior.Core.Assemblies.Assembly.Items["InFeed1-DMUnit"] as Experior.Catalog.Logistic.Track.DiverterMerger;
            InFeed1DMUnit.OnEnter += InFeed1DMUnit_OnEnter;

            Experior.Catalog.Logistic.Track.DiverterMerger InFeed2DMUnit = Experior.Core.Assemblies.Assembly.Items["InFeed2-DMUnit"] as Experior.Catalog.Logistic.Track.DiverterMerger;
            InFeed2DMUnit.OnEnter += InFeed2DMUnit_OnEnter;

            Experior.Catalog.Logistic.Track.DiverterMerger OutFeedDMUnit = Experior.Core.Assemblies.Assembly.Items["OutFeed-DMUnit"] as Experior.Catalog.Logistic.Track.DiverterMerger;
            OutFeedDMUnit.OnEnter += OutFeedDMUnit_OnEnter;

            Experior.Catalog.Logistic.Track.DiverterMerger SP11 = Experior.Core.Assemblies.Assembly.Items["SP11"] as Experior.Catalog.Logistic.Track.DiverterMerger;
            SP11.OnEnter += SP_OnEnter;
            Experior.Catalog.Logistic.Track.DiverterMerger SP12 = Experior.Core.Assemblies.Assembly.Items["SP12"] as Experior.Catalog.Logistic.Track.DiverterMerger;
            SP12.OnEnter += SP_OnEnter;
            Experior.Catalog.Logistic.Track.DiverterMerger SP21 = Experior.Core.Assemblies.Assembly.Items["SP21"] as Experior.Catalog.Logistic.Track.DiverterMerger;
            SP21.OnEnter += SP_OnEnter;
            Experior.Catalog.Logistic.Track.DiverterMerger SP22 = Experior.Core.Assemblies.Assembly.Items["SP22"] as Experior.Catalog.Logistic.Track.DiverterMerger;
            SP22.OnEnter += SP_OnEnter;

            ActionPoint.Get("SP11").OnEnter += new ActionPoint.EnterEvent((ActionPoint sender, Load load) => { load.MoveTo("PLA1SPL12"); load.Release(); });
            ActionPoint.Get("SP12").OnEnter += new ActionPoint.EnterEvent((ActionPoint sender, Load load) => { load.MoveTo("PLA1SPL22"); load.Release(); });
            ActionPoint.Get("SP21").OnEnter += new ActionPoint.EnterEvent((ActionPoint sender, Load load) => { load.MoveTo("PLA2SPL12"); load.Release(); });
            ActionPoint.Get("SP22").OnEnter += new ActionPoint.EnterEvent((ActionPoint sender, Load load) => { load.MoveTo("PLA2SPL22"); load.Release(); });

            Reset();
        }

        private bool SP_OnEnter(Catalog.Logistic.Track.DiverterMerger sender, Load load)
        {
            load.Stop();
            return true;
        }

        private bool InFeed1DMUnit_OnEnter(Catalog.Logistic.Track.DiverterMerger sender, Load load)
        {
            PalletUserData pls = load.UserData as PalletUserData;
            if (pls.Aisle == 1)
            {
                load.MoveTo("TCar1-InFeed");
                return true;
            }
            else
            {
                ActionPoint.Get("InFeed-TUnit").Parent.Motor.Forward();
                load.MoveTo("InFeed-TUnit");
                return true;
            }
        }

        private bool InFeed2DMUnit_OnEnter(Catalog.Logistic.Track.DiverterMerger sender, Load load)
        {
            PalletUserData pls = load.UserData as PalletUserData;
            if (pls.Aisle == 1)
            {
                ActionPoint.Get("InFeed-TUnit").Parent.Motor.Backward();
                load.MoveTo("InFeed-TUnit");
                return true;
            }
            else
            {
                load.MoveTo("TCar2-InFeed");
                return true;
            }
        }

        private bool OutFeedDMUnit_OnEnter(Catalog.Logistic.Track.DiverterMerger sender, Load load)
        {
            load.MoveTo("OutFeed");
            return true;
        }

        protected override void Arriving(INode node, Load load)
        {
            base.Arriving(node, load);

            if (node.Name == "TCar2-InFeed")
            {
                transferCarController2.PickupAndDrop(ActionPoint.Get("TCar2-InFeed"), ActionPoint.Get(GetPalletLocationName(load)));
            }

            if (node.Name == "TCar1-InFeed")
            {
                transferCarController1.PickupAndDrop(ActionPoint.Get("TCar1-InFeed"), ActionPoint.Get(GetPalletLocationName(load)));
            }

            if (node.Name == "PLA1SPL12")
            {
                ActionPoint.Get("PLA1SPL11").UserData = null;
                transferCarController1.PickupAndDrop(ActionPoint.Get(node.Name), ActionPoint.Get(GetPalletLocationName(load)));
            }
            if (node.Name == "PLA1SPL22")
            {
                ActionPoint.Get("PLA1SPL21").UserData = null;
                transferCarController1.PickupAndDrop(ActionPoint.Get(node.Name), ActionPoint.Get(GetPalletLocationName(load)));
            }

            if (node.Name == "PLA2SPL12")
            {
                ActionPoint.Get("PLA2SPL11").UserData = null;
                transferCarController2.PickupAndDrop(ActionPoint.Get(node.Name), ActionPoint.Get(GetPalletLocationName(load)));
            }
            if (node.Name == "PLA2SPL22")
            {
                ActionPoint.Get("PLA2SPL21").UserData = null;
                transferCarController2.PickupAndDrop(ActionPoint.Get(node.Name), ActionPoint.Get(GetPalletLocationName(load)));
            }

            if (node.Name == "PLA2EPL1" || node.Name == "PLA1EPL1")
            {
                Random rnd = null;
                bool pickCondition = (ActionPoint.Get("PLA1SPL11").UserData != null
             && ActionPoint.Get("PLA1SPL21").UserData != null
             && ActionPoint.Get("PLA2SPL11").UserData != null
             && ActionPoint.Get("PLA2SPL21").UserData != null);
                if (pickCondition)
                {
                    rnd = new Random();
                    OrderData tempOD = new OrderData((byte)rnd.Next(0, 3), (byte)rnd.Next(0, 3), (byte)rnd.Next(0, 3), (byte)rnd.Next(0, 3));
                    load.UserData = tempOD;
                    float sST = 8f;
                    float tST = tempOD.SKU1 * sST + tempOD.SKU2 * sST + tempOD.SKU3 * sST + tempOD.SKU4 * sST;
                    var l1H = ((Load)ActionPoint.Get("PLA1SPL11").UserData).Height / (((Load)ActionPoint.Get("PLA1SPL11").UserData).UserData as PalletUserData).LayerCount;
                    var l2H = ((Load)ActionPoint.Get("PLA1SPL21").UserData).Height / (((Load)ActionPoint.Get("PLA1SPL21").UserData).UserData as PalletUserData).LayerCount;
                    var l3H = ((Load)ActionPoint.Get("PLA2SPL11").UserData).Height / (((Load)ActionPoint.Get("PLA2SPL11").UserData).UserData as PalletUserData).LayerCount;
                    var l4H = ((Load)ActionPoint.Get("PLA2SPL21").UserData).Height / (((Load)ActionPoint.Get("PLA2SPL21").UserData).UserData as PalletUserData).LayerCount;

                    (((Load)ActionPoint.Get("PLA1SPL11").UserData).UserData as PalletUserData).LayerCount -= tempOD.SKU1;
                    (((Load)ActionPoint.Get("PLA1SPL21").UserData).UserData as PalletUserData).LayerCount -= tempOD.SKU2;
                    (((Load)ActionPoint.Get("PLA2SPL11").UserData).UserData as PalletUserData).LayerCount -= tempOD.SKU3;
                    (((Load)ActionPoint.Get("PLA2SPL21").UserData).UserData as PalletUserData).LayerCount -= tempOD.SKU4;

                    Experior.Core.Loads.Load l1Load = null;
                    Experior.Core.Loads.Load l2Load = null;
                    Experior.Core.Loads.Load l3Load = null;
                    Experior.Core.Loads.Load l4Load = null;

                    Experior.Core.Timer.Action(()=> 
                    {
                        l1Load = Experior.Core.Loads.Load.CreateBox(1, tempOD.SKU1 * l1H, 1.2f, ((Load)ActionPoint.Get("PLA1SPL11").UserData).Color);
                        load.Group(l1Load, new Microsoft.DirectX.Vector3(0, load.Height / 2 + l1Load.Height / 2, 0));
                        Experior.Core.Timer.Action(() =>
                        {
                            l2Load = Experior.Core.Loads.Load.CreateBox(1, tempOD.SKU2 * l2H, 1.2f, ((Load)ActionPoint.Get("PLA1SPL21").UserData).Color);
                            load.Group(l2Load, new Microsoft.DirectX.Vector3(0, load.Height / 2 + l1Load.Height + l2Load.Height / 2, 0));
                            Experior.Core.Timer.Action(() =>
                            {
                                l3Load = Experior.Core.Loads.Load.CreateBox(1, tempOD.SKU3 * l3H, 1.2f, ((Load)ActionPoint.Get("PLA2SPL11").UserData).Color);
                                load.Group(l3Load, new Microsoft.DirectX.Vector3(0, load.Height / 2 + l1Load.Height + l2Load.Height + l3Load.Height / 2, 0));
                                Experior.Core.Timer.Action(() =>
                                {
                                    l4Load = Experior.Core.Loads.Load.CreateBox(1, tempOD.SKU4 * l4H, 1.2f, ((Load)ActionPoint.Get("PLA2SPL21").UserData).Color);
                                    load.Group(l4Load, new Microsoft.DirectX.Vector3(0, load.Height / 2 + l1Load.Height + l2Load.Height + l3Load.Height + l4Load.Height / 2, 0));
                                }, tempOD.SKU4 * sST);
                            }, tempOD.SKU3 * sST);
                        }, tempOD.SKU2 * sST);
                    }, tempOD.SKU1 * sST);

                    //l1Load = Experior.Core.Loads.Load.CreateBox(1, tempOD.SKU1 * l1H, 1.2f, ((Load)ActionPoint.Get("PLA1SPL11").UserData).Color);
                    //l2Load = Experior.Core.Loads.Load.CreateBox(1, tempOD.SKU2 * l2H, 1.2f, ((Load)ActionPoint.Get("PLA1SPL21").UserData).Color);
                    //l3Load = Experior.Core.Loads.Load.CreateBox(1, tempOD.SKU3 * l3H, 1.2f, ((Load)ActionPoint.Get("PLA2SPL11").UserData).Color);
                    //l4Load = Experior.Core.Loads.Load.CreateBox(1, tempOD.SKU4 * l4H, 1.2f, ((Load)ActionPoint.Get("PLA2SPL21").UserData).Color);

                    //load.Group(l1Load, new Microsoft.DirectX.Vector3(0, load.Height / 2 + l1Load.Height / 2, 0));
                    //load.Group(l2Load, new Microsoft.DirectX.Vector3(0, load.Height / 2 + l1Load.Height + l2Load.Height / 2, 0));
                    //load.Group(l3Load, new Microsoft.DirectX.Vector3(0, load.Height / 2 + l1Load.Height + l2Load.Height + l3Load.Height / 2, 0));
                    //load.Group(l4Load, new Microsoft.DirectX.Vector3(0, load.Height / 2 + l1Load.Height + l2Load.Height + l3Load.Height + l4Load.Height / 2, 0));
                }
            }
        }

        private void Controller_OnEnter(ActionPoint sender, Load load)
        {
            throw new NotImplementedException();
        }

        private string GetPalletLocationName(Load load)
        {
            return GetPalletStorageName(load.UserData as PalletUserData);
        }

        private string GetPalletStorageName(PalletUserData plSTInfo)
        {
            string destName = "PLA" + plSTInfo.Aisle + "L" + plSTInfo.Location;
            return destName;
        }

        protected override void KeyDown(KeyEventArgs e)
        {
            if (Core.Environment.InvokeRequired)
            {
                Core.Environment.Invoke(() => KeyDown(e));
                return;
            }

            base.KeyDown(e);
            Random rnd = new Random();

            switch (e.KeyCode)
            {
                case Keys.NumPad1:
                case Keys.NumPad2:
                    InFeedMethod(e, rnd);
                    break;
                case Keys.NumPad3:
                    foreach (var item in aisle1Locations)
                    {
                        if (!item.Value && item.Key.ActiveLoad == null)
                        {
                            Experior.Core.Loads.Load.CreateEuroPallet().Switch(item.Key);
                        }
                    }
                    break;
                case Keys.NumPad4:
                    foreach (var item in aisle2Locations)
                    {
                        if (!item.Value && item.Key.ActiveLoad == null)
                        {
                            Experior.Core.Loads.Load.CreateEuroPallet().Switch(item.Key);
                        }
                    }
                    break;
                case Keys.NumPad5:
                    foreach (var item in aisle1Locations)
                    {
                        if (item.Key.ActiveLoad != null && item.Key.ActiveLoad is EuroPallet)
                        {
                            transferCarController1.PickupAndDrop(item.Key, ActionPoint.Get("PLA1EPL1"));
                            break;
                        }
                    }
                    break;
                case Keys.NumPad6:
                    if (ActionPoint.Get("PLA2EPL1").ActiveLoad != null)
                    {
                        break;
                    }
                    foreach (var item in aisle2Locations)
                    {
                        if (item.Key.ActiveLoad != null && item.Key.ActiveLoad is EuroPallet)
                        {
                            transferCarController2.PickupAndDrop(item.Key, ActionPoint.Get("PLA2EPL1"));
                            break;
                        }
                    }
                    break;
                case Keys.NumPad7:
                    if (ActionPoint.Get("PLA1EPL1").ActiveLoad != null)
                    {
                        transferCarController1.PickupAndDrop(ActionPoint.Get("PLA1EPL1"), ActionPoint.Get("TCar1-OutFeed"));
                    }
                    break;
                case Keys.NumPad8:
                    if (ActionPoint.Get("PLA2EPL1").ActiveLoad != null)
                    {
                        transferCarController2.PickupAndDrop(ActionPoint.Get("PLA2EPL1"), ActionPoint.Get("TCar2-OutFeed"));
                    }
                    break;

                case Keys.Z:
                    ActionPoint tempAPA1 = null;
                    foreach (var item in aisle1Locations)
                    {
                        if (item.Value)
                        {
                            if (ActionPoint.Get("PLA1SPL11").UserData == null)
                            {
                                ActionPoint.Get("PLA1SPL11").UserData = item.Key.ActiveLoad;
                                transferCarController1.PickupAndDrop(item.Key, ActionPoint.Get("PLA1SPL11"));
                                tempAPA1 = item.Key;
                                break;
                            }
                            if (ActionPoint.Get("PLA1SPL21").UserData == null)
                            {
                                ActionPoint.Get("PLA1SPL21").UserData = item.Key.ActiveLoad;
                                transferCarController1.PickupAndDrop(item.Key, ActionPoint.Get("PLA1SPL21"));
                                tempAPA1 = item.Key;
                                break;
                            }
                            break;
                        }
                    }
                    if (tempAPA1 != null)
                    {
                        aisle1Locations[tempAPA1] = false;
                    }

                    break;

                case Keys.X:
                    ActionPoint tempAPA2 = null;
                    foreach (var item in aisle2Locations)
                    {
                        if (item.Value)
                        {
                            if (ActionPoint.Get("PLA2SPL11").UserData == null)
                            {
                                ActionPoint.Get("PLA2SPL11").UserData = item.Key.ActiveLoad;

                                transferCarController2.PickupAndDrop(item.Key, ActionPoint.Get("PLA2SPL11"));
                                tempAPA2 = item.Key;
                                break;
                            }
                            if (ActionPoint.Get("PLA2SPL21").UserData == null)
                            {
                                ActionPoint.Get("PLA2SPL21").UserData = item.Key.ActiveLoad;
                                transferCarController2.PickupAndDrop(item.Key, ActionPoint.Get("PLA2SPL21"));
                                tempAPA2 = item.Key;
                                break;
                            }
                            break;
                        }
                    }
                    if (tempAPA2 != null)
                    {
                        aisle2Locations[tempAPA2] = false;
                    }
                    break;


                case Keys.C:
                    ((Load)ActionPoint.Get("PLA1SPL11").UserData)?.Release();
                    ((Load)ActionPoint.Get("PLA1SPL21").UserData)?.Release();
                    ((Load)ActionPoint.Get("PLA2SPL11").UserData)?.Release();
                    ((Load)ActionPoint.Get("PLA2SPL21").UserData)?.Release();
                    break;
                default:
                    break;
            }
        }

        private void InFeedMethod(KeyEventArgs e, Random rnd)
        {
            float pHeightVar = (byte)(rnd).Next(1, 5);
            float pHeight = 1 + 0.25f * pHeightVar;
            Load tempPallet = Experior.Core.Loads.Load.CreateBox(1, pHeight, 1.2f, System.Drawing.Color.FromArgb((byte)(rnd).Next(1, 255), (byte)(rnd).Next(1, 255), (byte)(rnd).Next(1, 255)));
            //tempPallet.Yaw += (float)(Math.PI / 2);
            PalletUserData tempPalletStorage = null;
            var layer = (byte)(rnd).Next(4, 7);
            while (true)
            {
                var aisle = (byte)(rnd).Next(1, 3);
                var location = (byte)(rnd).Next(1, 28);
                tempPalletStorage = new PalletUserData(aisle, location, layer);
                if (aisle == 1)
                {
                    if (aisle1Locations[ActionPoint.Get(GetPalletStorageName(tempPalletStorage))] == true)
                    {
                        continue;
                    }
                    else
                    {
                        aisle1Locations[ActionPoint.Get(GetPalletStorageName(tempPalletStorage))] = true;
                        break;
                    }
                }
                if (aisle == 2)
                {
                    if (aisle2Locations[ActionPoint.Get(GetPalletStorageName(tempPalletStorage))] == true)
                    {
                        continue;
                    }
                    else
                    {
                        aisle2Locations[ActionPoint.Get(GetPalletStorageName(tempPalletStorage))] = true;
                        break;
                    }
                }
            }

            tempPallet.UserData = tempPalletStorage;

            if (e.KeyData == Keys.NumPad1)
            {
                ActionPoint.Get("InFeed1").Parent.Add(tempPallet);
                return;
            }
            else if (e.KeyData == Keys.NumPad2)
            {
                ActionPoint.Get("InFeed2").Parent.Add(tempPallet);
                return;
            }

        }

        protected override void Reset()
        {
            base.Reset();

            aisle1Locations = new Dictionary<ActionPoint, bool>();
            foreach (var ap in ActionPoint.Items)
            {
                if (ap.Value.Name.Contains("PLA1L"))
                {
                    aisle1Locations.Add(ap.Value, false);
                }
            }

            aisle2Locations = new Dictionary<ActionPoint, bool>();
            foreach (var ap in ActionPoint.Items)
            {
                if (ap.Value.Name.Contains("PLA2L"))
                {
                    aisle2Locations.Add(ap.Value, false);
                }
            }

            ActionPoint.Get("PLA1SPL11").UserData = null;
            ActionPoint.Get("PLA1SPL21").UserData = null;
            ActionPoint.Get("PLA2SPL11").UserData = null;
            ActionPoint.Get("PLA2SPL21").UserData = null;
        }

    }
    public class PalletUserData
    {
        public byte Aisle { get; set; }
        public byte Location { get; set; }

        public uint LayerCount { get; set; }
        public string Sku { get; set; }

        public PalletUserData(byte aisle, byte location, uint layerCount)
        {
            Aisle = aisle;
            Location = location;
            LayerCount = layerCount;
        }

        public override string ToString()
        {
            return $"Layer count: {LayerCount}, Aisle: {Aisle}";
        }
    }

    public class OrderData
    {
        public byte SKU1 { get; set; }
        public byte SKU2 { get; set; }
        public byte SKU3 { get; set; }
        public byte SKU4 { get; set; }
        public OrderData(byte sku1, byte sku2, byte sku3, byte sku4)
        {
            SKU1 = sku1;
            SKU2 = sku2;
            SKU3 = sku4;
            SKU4 = sku4;
        }
    }
}