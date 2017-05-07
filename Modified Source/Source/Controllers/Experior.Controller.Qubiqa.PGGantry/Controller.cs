using System.Windows.Forms;
using Experior.Catalog.Qubiqa.TransferCar.Assemblies;
using Experior.Core.Loads;
using Experior.Core.Routes;
using System;
using System.Drawing;
using System.Collections.Generic;
using System.IO;

namespace Experior.Controller.Qubiqa.PGGantry
{
    public class Controller : Core.Controller
    {
        private TransferCarController transferCarController1;
        private TransferCarController transferCarController2;

        private List<KeyValuePair<ActionPoint, bool>> aisle1Locations;
        private List<KeyValuePair<ActionPoint, bool>> aisle2Locations;

        private Experior.Core.Timer RefreshCycle;

        private List<string> skuDataList;
        private List<SKU> skuList;
        private List<SKUSupply> skuSupplyList;
        private List<PalletRequest> palletRequestList;

        private Random rndFeed;

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

            //Experior.Catalog.Logistic.Track.DiverterMerger SP11 = Experior.Core.Assemblies.Assembly.Items["SP11"] as Experior.Catalog.Logistic.Track.DiverterMerger;
            //SP11.OnEnter += SP_OnEnter;
            //Experior.Catalog.Logistic.Track.DiverterMerger SP12 = Experior.Core.Assemblies.Assembly.Items["SP12"] as Experior.Catalog.Logistic.Track.DiverterMerger;
            //SP12.OnEnter += SP_OnEnter;
            //Experior.Catalog.Logistic.Track.DiverterMerger SP21 = Experior.Core.Assemblies.Assembly.Items["SP21"] as Experior.Catalog.Logistic.Track.DiverterMerger;
            //SP21.OnEnter += SP_OnEnter;
            //Experior.Catalog.Logistic.Track.DiverterMerger SP22 = Experior.Core.Assemblies.Assembly.Items["SP22"] as Experior.Catalog.Logistic.Track.DiverterMerger;
            //SP22.OnEnter += SP_OnEnter;

            //ActionPoint.Get("SP11").OnEnter += new ActionPoint.EnterEvent((ActionPoint sender, Load load) => { load.MoveTo("PLA1SPL12"); load.Release(); });
            //ActionPoint.Get("SP12").OnEnter += new ActionPoint.EnterEvent((ActionPoint sender, Load load) => { load.MoveTo("PLA1SPL22"); load.Release(); });
            //ActionPoint.Get("SP21").OnEnter += new ActionPoint.EnterEvent((ActionPoint sender, Load load) => { load.MoveTo("PLA2SPL12"); load.Release(); });
            //ActionPoint.Get("SP22").OnEnter += new ActionPoint.EnterEvent((ActionPoint sender, Load load) => { load.MoveTo("PLA2SPL22"); load.Release(); });

            //Stores the arrived pallet in Aisle 1
            ActionPoint.Items["TCar1-InFeed"].OnEnter += TCar1InFeed_AP_OnEnter;
            //Stores the arrived pallet Aisle 2
            ActionPoint.Items["TCar2-InFeed"].OnEnter += TCar2InFeed_AP_OnEnter;
            //Returns pallet to the inventory and updates its status
            ActionPoint.Items["OutFeed"].OnEnter += OutFeed_AP_OnEnter;
            

            RefreshCycle = new Core.Timer(5.0f);
            RefreshCycle.AutoReset = true;
            RefreshCycle.OnElapsed += RefreshCycle_OnElapsed;

            rndFeed = new Random();

            Reset();
        }

        //Stores the arrived pallet in Aisle 1
        private void TCar1InFeed_AP_OnEnter(ActionPoint sender, Load load)
        {
            ActionPoint storageDestination = findUnoccupiedLocation(load, aisle1Locations);
            transferCarController1.PickupAndDrop(sender, storageDestination);

        }

        //Stores the arrived pallet in Aisle 2
        private void TCar2InFeed_AP_OnEnter(ActionPoint sender, Load load)
        {
            ActionPoint storageDestination = findUnoccupiedLocation(load, aisle2Locations);
            transferCarController2.PickupAndDrop(sender,storageDestination);
        }

        //Returns the pallet to the inventory and updates its status
        private void OutFeed_AP_OnEnter(ActionPoint sender, Load load)
        {
            Pallet palletData = load.UserData as Pallet;
            SKUSupply skuSupply = skuSupplyList.Find(a => a.SKUData.ItemCode == palletData.Content.ItemCode);
            skuSupply.ReturnPallet(palletData);
        }

        private void RefreshCycle_OnElapsed(Core.Timer sender)
        {
            Log.Write(DateTime.Now.ToString());
            //TODO:Request palleter load back
            foreach (var item in palletRequestList)
            {
                InFeedMethod(item);
            }
        }

        private bool SP_OnEnter(Catalog.Logistic.Track.DiverterMerger sender, Load load)
        {
            load.Stop();
            return true;
        }

        //Sends pallet to Aisle 1
        private bool InFeed1DMUnit_OnEnter(Catalog.Logistic.Track.DiverterMerger sender, Load load)
        {
            load.MoveTo("TCar1-InFeed");
            return true;
        }

        //Sends pallet to Aisle 2
        private bool InFeed2DMUnit_OnEnter(Catalog.Logistic.Track.DiverterMerger sender, Load load)
        {
            load.MoveTo("TCar2-InFeed");
            return true;
        }

        private bool OutFeedDMUnit_OnEnter(Catalog.Logistic.Track.DiverterMerger sender, Load load)
        {
            load.MoveTo("OutFeed");
            return true;
        }

        protected override void Arriving(INode node, Load load)
        {
            base.Arriving(node, load);

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

                    Experior.Core.Timer.Action(() =>
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

        private string GetPalletLocationName(Load load)
        {
            return GetPalletStorageName(load.UserData as PalletUserData);
        }

        private string GetPalletStorageName(PalletUserData plSTInfo)
        {
            string destName = "PLA" + plSTInfo.Aisle + "L" + plSTInfo.Location;
            return destName;
        }

        //protected override void KeyDown(KeyEventArgs e)
        //{
        //    if (Core.Environment.InvokeRequired)
        //    {
        //        Core.Environment.Invoke(() => KeyDown(e));
        //        return;
        //    }

        //    base.KeyDown(e);
        //    Random rnd = new Random();

        //    switch (e.KeyCode)
        //    {
        //        case Keys.NumPad1:
        //        case Keys.NumPad2:
        //            InFeedMethod(e, rnd);
        //            break;
        //        case Keys.NumPad3:
        //            foreach (var item in aisle1Locations)
        //            {
        //                if (!item.Value && item.Key.ActiveLoad == null)
        //                {
        //                    Experior.Core.Loads.Load.CreateEuroPallet().Switch(item.Key);
        //                }
        //            }
        //            break;
        //        case Keys.NumPad4:
        //            foreach (var item in aisle2Locations)
        //            {
        //                if (!item.Value && item.Key.ActiveLoad == null)
        //                {
        //                    Experior.Core.Loads.Load.CreateEuroPallet().Switch(item.Key);
        //                }
        //            }
        //            break;
        //        case Keys.NumPad5:
        //            foreach (var item in aisle1Locations)
        //            {
        //                if (item.Key.ActiveLoad != null && item.Key.ActiveLoad is EuroPallet)
        //                {
        //                    transferCarController1.PickupAndDrop(item.Key, ActionPoint.Get("PLA1EPL1"));
        //                    break;
        //                }
        //            }
        //            break;
        //        case Keys.NumPad6:
        //            if (ActionPoint.Get("PLA2EPL1").ActiveLoad != null)
        //            {
        //                break;
        //            }
        //            foreach (var item in aisle2Locations)
        //            {
        //                if (item.Key.ActiveLoad != null && item.Key.ActiveLoad is EuroPallet)
        //                {
        //                    transferCarController2.PickupAndDrop(item.Key, ActionPoint.Get("PLA2EPL1"));
        //                    break;
        //                }
        //            }
        //            break;
        //        case Keys.NumPad7:
        //            if (ActionPoint.Get("PLA1EPL1").ActiveLoad != null)
        //            {
        //                transferCarController1.PickupAndDrop(ActionPoint.Get("PLA1EPL1"), ActionPoint.Get("TCar1-OutFeed"));
        //            }
        //            break;
        //        case Keys.NumPad8:
        //            if (ActionPoint.Get("PLA2EPL1").ActiveLoad != null)
        //            {
        //                transferCarController2.PickupAndDrop(ActionPoint.Get("PLA2EPL1"), ActionPoint.Get("TCar2-OutFeed"));
        //            }
        //            break;

        //        case Keys.Z:
        //            ActionPoint tempAPA1 = null;
        //            foreach (var item in aisle1Locations)
        //            {
        //                if (item.Value)
        //                {
        //                    if (ActionPoint.Get("PLA1SPL11").UserData == null)
        //                    {
        //                        ActionPoint.Get("PLA1SPL11").UserData = item.Key.ActiveLoad;
        //                        transferCarController1.PickupAndDrop(item.Key, ActionPoint.Get("PLA1SPL11"));
        //                        tempAPA1 = item.Key;
        //                        break;
        //                    }
        //                    if (ActionPoint.Get("PLA1SPL21").UserData == null)
        //                    {
        //                        ActionPoint.Get("PLA1SPL21").UserData = item.Key.ActiveLoad;
        //                        transferCarController1.PickupAndDrop(item.Key, ActionPoint.Get("PLA1SPL21"));
        //                        tempAPA1 = item.Key;
        //                        break;
        //                    }
        //                    break;
        //                }
        //            }
        //            if (tempAPA1 != null)
        //            {
        //                aisle1Locations[tempAPA1] = false;
        //            }

        //            break;

        //        case Keys.X:
        //            ActionPoint tempAPA2 = null;
        //            foreach (var item in aisle2Locations)
        //            {
        //                if (item.Value)
        //                {
        //                    if (ActionPoint.Get("PLA2SPL11").UserData == null)
        //                    {
        //                        ActionPoint.Get("PLA2SPL11").UserData = item.Key.ActiveLoad;

        //                        transferCarController2.PickupAndDrop(item.Key, ActionPoint.Get("PLA2SPL11"));
        //                        tempAPA2 = item.Key;
        //                        break;
        //                    }
        //                    if (ActionPoint.Get("PLA2SPL21").UserData == null)
        //                    {
        //                        ActionPoint.Get("PLA2SPL21").UserData = item.Key.ActiveLoad;
        //                        transferCarController2.PickupAndDrop(item.Key, ActionPoint.Get("PLA2SPL21"));
        //                        tempAPA2 = item.Key;
        //                        break;
        //                    }
        //                    break;
        //                }
        //            }
        //            if (tempAPA2 != null)
        //            {
        //                aisle2Locations[tempAPA2] = false;
        //            }
        //            break;


        //        case Keys.C:
        //            ((Load)ActionPoint.Get("PLA1SPL11").UserData)?.Release();
        //            ((Load)ActionPoint.Get("PLA1SPL21").UserData)?.Release();
        //            ((Load)ActionPoint.Get("PLA2SPL11").UserData)?.Release();
        //            ((Load)ActionPoint.Get("PLA2SPL21").UserData)?.Release();
        //            break;
        //        default:
        //            break;
        //    }
        //}

        //private void InFeedMethod(KeyEventArgs e, Random rnd)
        //{
        //    float pHeightVar = (byte)(rnd).Next(1, 5);
        //    float pHeight = 1 + 0.25f * pHeightVar;
        //    Load tempPallet = Experior.Core.Loads.Load.CreateBox(1, pHeight, 1.2f, System.Drawing.Color.FromArgb((byte)(rnd).Next(1, 255), (byte)(rnd).Next(1, 255), (byte)(rnd).Next(1, 255)));
        //    //tempPallet.Yaw += (float)(Math.PI / 2);
        //    PalletUserData tempPalletStorage = null;
        //    var layer = (byte)(rnd).Next(4, 7);
        //    while (true)
        //    {
        //        var aisle = (byte)(rnd).Next(1, 3);
        //        var location = (byte)(rnd).Next(1, 28);
        //        tempPalletStorage = new PalletUserData(aisle, location, layer);
        //        if (aisle == 1)
        //        {
        //            if (aisle1Locations[ActionPoint.Get(GetPalletStorageName(tempPalletStorage))] == true)
        //            {
        //                continue;
        //            }
        //            else
        //            {
        //                aisle1Locations[ActionPoint.Get(GetPalletStorageName(tempPalletStorage))] = true;
        //                break;
        //            }
        //        }
        //        if (aisle == 2)
        //        {
        //            if (aisle2Locations[ActionPoint.Get(GetPalletStorageName(tempPalletStorage))] == true)
        //            {
        //                continue;
        //            }
        //            else
        //            {
        //                aisle2Locations[ActionPoint.Get(GetPalletStorageName(tempPalletStorage))] = true;
        //                break;
        //            }
        //        }
        //    }

        //    tempPallet.UserData = tempPalletStorage;

        //    if (e.KeyData == Keys.NumPad1)
        //    {
        //        ActionPoint.Get("InFeed1").Parent.Add(tempPallet);
        //        return;
        //    }
        //    else if (e.KeyData == Keys.NumPad2)
        //    {
        //        ActionPoint.Get("InFeed2").Parent.Add(tempPallet);
        //        return;
        //    }

        //}

        private void InFeedMethod(PalletRequest request)
        {
            if (request.Status == PalletRequestStatus.Requested)
            {
                return;
            }
            float LoadHeight = .25f;
            foreach (var item in request.RequestPallet())
            {
                Experior.Core.Loads.Load load = Experior.Core.Loads.Load.CreateBox(1f, item.LayerCount * LoadHeight,1.2f, Color.Azure);
                load.UserData = item;
                int i = rndFeed.Next(1, 3);
                if (i == 1)
                {
                    load.Switch(ActionPoint.Get("InFeed1").Parent);
                }
                else
                {
                    load.Switch(ActionPoint.Get("InFeed2").Parent);
                }
            }

        }

        protected override void Reset()
        {
            if (Experior.Core.Environment.InvokeRequired)
            {
                Experior.Core.Environment.Invoke(() => { Reset(); });
            }
            base.Reset();

            GetStorageLocations(ref aisle1Locations, 1);
            GetStorageLocations(ref aisle2Locations, 2);

            //ActionPoint.Get("PLA1SPL11").UserData = null;
            //ActionPoint.Get("PLA1SPL21").UserData = null;
            //ActionPoint.Get("PLA2SPL11").UserData = null;
            //ActionPoint.Get("PLA2SPL21").UserData = null;

            skuDataList = new List<string>();
            skuList = new List<SKU>();
            skuSupplyList = new List<SKUSupply>();
            palletRequestList = new List<PalletRequest>();

            RefreshCycle.Reset();
            RefreshCycle.Start();

            using (StreamReader a1 = new StreamReader(@"D:\Projects Vault\Qubiqa-P-G\GitHUB\Test Scenarios\TS1\Inventory.csv"))
            {
                a1.ReadLine();
                for (string i = a1.ReadLine(); i != null; i = a1.ReadLine())
                {
                    skuDataList.Add(i);
                    skuList.Add(GenerateSKU(i));
                    skuSupplyList.Add(GenerateSKUSupply(i));
                }
            }

            using (StreamReader a1 = new StreamReader(@"D:\Projects Vault\Qubiqa-P-G\GitHUB\Test Scenarios\TS1\SKURequest.csv"))
            {
                a1.ReadLine();
                for (string i = a1.ReadLine(); i != null; i = a1.ReadLine())
                {
                    palletRequestList.Add(GeneratePalleteRequest(i, skuSupplyList));
                }
            }


            //TODO:Send load back
            //for (int i = 0; i < skuSupplyList.Count; i++)
            //{

            //    for (int j = 0; j < skuSupplyList[i].PalletList.Count; j++)
            //    {
            //        skuSupplyList[i].ReturnPallet(skuSupplyList[i].PalletList[j].Key);
            //    }
            //}
        }


#region Auxiliary Methods

        //Gets the storage location in the relevant aisle
        private void GetStorageLocations(ref List<KeyValuePair<ActionPoint, bool>> aisleLocations, byte aisleNumber)
        {
            aisleLocations = new List<KeyValuePair<ActionPoint, bool>>();
            string aisleLoc= "PL-A" + aisleNumber.ToString();
            foreach (var item in ActionPoint.Items)
            {
                if (item.Key.Contains(aisleLoc))
                {
                    aisleLocations.Add(new KeyValuePair<ActionPoint, bool>(item.Value, true));
                    item.Value.OnEnter += StorageLocation_OnEnter;
                }
            }
        }

        //Finds an empty storage location in the relevant aisle to store the pallet
        private ActionPoint findUnoccupiedLocation(Load load, List<KeyValuePair<ActionPoint, bool>> aisleLocations)
        {
            ActionPoint destination = null;
            for (int i = 0; i < aisleLocations.Count; i++)
            {
                if (aisleLocations[i].Value == true)
                {
                    aisleLocations[i] = new KeyValuePair<ActionPoint, bool>(aisleLocations[i].Key, false);
                    destination = aisleLocations[i].Key;
                    return destination;
                }
            }
            return destination;
        }


        private void StorageLocation_OnEnter(ActionPoint sender, Load load)
        {
           // Pallet pallet = load.UserData as pallet;

        }
        #endregion Auxiliary Methods


        private SKU GenerateSKU(string SKUData)
        {
            SKU newSKU = null;

            var skuData = SKUData.Split(',');
            string itemIndex = skuData[0];
            string itemCode = skuData[1];
            SKUClass itemClass;
            Enum.TryParse<SKUClass>(skuData[3], out itemClass);
            byte casePerLayer = byte.Parse(skuData[5]);
            byte layerPerPallet = byte.Parse(skuData[6]);
            byte layerPerPick = byte.Parse(skuData[8]);

            newSKU = new SKU(itemIndex, itemCode, itemClass, casePerLayer, layerPerPallet, layerPerPick);

            return newSKU;
        }

        private SKUSupply GenerateSKUSupply(string SKUData)
        {
            SKUSupply newSKUSupply = null;
            SKU newSKU = GenerateSKU(SKUData);
            var skuData = SKUData.Split(',');
            int layerPerDay = int.Parse(skuData[7]);
            int palletPerDay = int.Parse(skuData[9]);
            newSKUSupply = new SKUSupply(newSKU, layerPerDay, palletPerDay);
            return newSKUSupply;
        }

        private PalletRequest GeneratePalleteRequest(string PalletRequestData, List<SKUSupply> SKUSupplyList)
        {
            PalletRequest newPalletRequest = null;
            var prData = PalletRequestData.Split(',');
            string itemCode = prData[1];
            int requiredLayerCount = int.Parse(prData[4]);
            newPalletRequest = new PalletRequest(SKUSupplyList, itemCode, requiredLayerCount);
            return newPalletRequest;
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