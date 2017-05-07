using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Experior.Controller.Qubiqa.PGGantry
{
    public enum SKUClass : byte { FastMoving = 0, SlowMoving = 1 }
    public enum PalletRequestStatus : byte { InQueue = 0, Requested = 1}
    public enum PickStackStatus { Waiting = 0, Preparing = 1, Available = 2, Done = 3}

    public class SKU
    {
        public string ItemIndex { get; private set; }
        public string ItemCode { get; private set; }
        public SKUClass ItemClass { get; private set; }
        public byte CasePerLayer { get; private set; }
        public byte LayerPerPallete { get; private set; }
        public byte LayerPerPick { get; private set; }
        //TODO: layer height needs to be provided for visualization
        private float layerHeight { get; set; }

        public SKU(string ItemIndex, string ItemCode, SKUClass ItemClass, byte CasePerLayer, byte LayerPerPallete, byte LayerPerPick)
        {
            this.ItemIndex = ItemIndex;
            this.ItemCode = ItemCode;
            this.ItemClass = ItemClass;
            this.CasePerLayer = CasePerLayer;
            this.LayerPerPallete = LayerPerPallete;
            this.LayerPerPick = LayerPerPick;
        }
    }

    public class Pallet
    {
        public int LayerCount { get; private set; }
        public int WrappedLayers { get; private set; }
        public SKU Content { get; private set; }
        public Pallet(SKU content)
        {
            this.Content = content;
            this.LayerCount = content.LayerPerPallete;
            this.WrappedLayers = content.LayerPerPallete;
        }

        public Pallet(SKU content, int layer)
        {
            this.Content = content;
            this.LayerCount = layer;
            this.WrappedLayers = layer;
        }

        public void AddLayer(int layerCount)
        {
            this.LayerCount += layerCount;
        }

        public void RemoveLayer(int layerCount)
        {
            this.LayerCount -= layerCount;
        }

        public void Unwrap(int layerCount)
        {
            this.WrappedLayers -= layerCount;
        }

        public void Wrap()
        {
            this.WrappedLayers = LayerCount;
        }

        public override string ToString()
        {
            string palleteInfo = "";
            //palleteInfo += "SKU:" + Content.ItemCode + " - " + "Index:" + Content.ItemIndex + " - " + "Layer:" + LayerCount;
            palleteInfo += "SKU:" + Content.ItemCode + "\n" + "-" + "Index:" + Content.ItemIndex + " - " + "Layer:" + LayerCount;
            return palleteInfo;
        }
    }

    public class SKUSupply
    {
        public SKU SKUData { get; set; }
        public int LayerPerDayCount { get; set; }
        public int PalletPerDayCount { get; set; }
        //Total number of layers in the picking area at the storage locations and pick positions
        public int AvailableToPickLayerCount { get; set; }
        //Total number of layers in the picking area at the storage locations
        public int ReadyToPickLayerCount { get; set; }
        public int OnTheWayLayerCount { get; set; }
        public int ToBePickedLayerCount { get; set; }
        public int PickedLayerCount { get; set; }
        public int RemainingLayerCount { get; set; }
        public List<KeyValuePair<Pallet, bool>> PalletList { get; set; }

        public SKUSupply(SKU skuData, int layerPerDay, int palletPerDay)
        {
            this.SKUData = skuData;
            this.LayerPerDayCount = layerPerDay;
            this.PalletPerDayCount = palletPerDay;
            this.AvailableToPickLayerCount = 0;
            this.ReadyToPickLayerCount = 0;
            this.OnTheWayLayerCount = 0;
            this.ToBePickedLayerCount = 0;
            this.PickedLayerCount = 0;
            this.RemainingLayerCount = layerPerDay;

            this.PalletList = new List<KeyValuePair<Pallet, bool>>(palletPerDay);
            for (int i = 0; i < PalletList.Count - 1; i++)
            {
                PalletList.Add(new KeyValuePair<Pallet, bool>(new Pallet(SKUData), true));
            }
            var lastPalletLC = layerPerDay - skuData.LayerPerPallete * (palletPerDay - 1);
            var lastPallet = new Pallet(skuData, lastPalletLC);
            PalletList.Add(new KeyValuePair<Pallet, bool>(lastPallet, true));
        }

        //ToDo:
        private void UpdateSupply(int pickedLayer)
        {
            RemainingLayerCount = LayerPerDayCount - pickedLayer;
        }

        public List<Pallet> RequestLayer(int layer)
        {
            List<Pallet> requested = new List<Pallet>();
            if (layer==0)
            {
                return null;
            }
            else if (layer<=RemainingLayerCount)
            {
                int ToBeSentLayer = 0;
                for (int i = 0; i < PalletList.Count; i++)
                {
                    if ((!PalletList[i].Value) || (PalletList[i].Value && PalletList[i].Key.LayerCount == 0))
                    {
                        continue;
                    }
                    else
                    {
                        OnTheWayLayerCount += PalletList[i].Key.LayerCount;
                        RemainingLayerCount -= PalletList[i].Key.LayerCount;
                        ToBeSentLayer += PalletList[i].Key.LayerCount;
                        ChangePalletAvailability(PalletList[i].Key, false);
                        requested.Add(PalletList[i].Key);
                        if (ToBeSentLayer >= layer)
                        {
                            break;
                        }
                        else
                        {
                            continue;
                        }
                    }
                }
                return requested;
            }
            else if (layer > RemainingLayerCount)
            {
                int excessLayerCount = layer - RemainingLayerCount;
                int excessPalletCount = (int)(Math.Ceiling((double)excessLayerCount / (double)SKUData.LayerPerPallete));
                for (int i = 0; i < excessPalletCount - 1; i++)
                {
                    PalletList.Add(new KeyValuePair<Pallet, bool>(new Pallet(SKUData), true));
                }
                var lastExcessPalletLC = excessLayerCount - SKUData.LayerPerPallete * (excessPalletCount - 1);
                PalletList.Add(new KeyValuePair<Pallet, bool>(new Pallet(SKUData, lastExcessPalletLC), true));
                LayerPerDayCount += excessLayerCount;
                PalletPerDayCount += excessPalletCount;
                RemainingLayerCount += excessLayerCount;

                //requested = );
                return RequestLayer(layer);
            }
            else
            {
                return null;
            }     
        }
        public void ReturnPallet(Pallet usedPallet)
        {
            if (usedPallet.LayerCount > 0)
            {
                usedPallet.Wrap();
                ChangePalletAvailability(usedPallet, true);
                RemainingLayerCount += usedPallet.LayerCount;
            }
            else
            {
                ChangePalletAvailability(usedPallet, false);
            }
        }

        private void ChangePalletAvailability(Pallet Pallet, bool IsAvailable)
        {
            for (int i = 0; i < PalletList.Count; i++)
            {
                if (PalletList[i].Key == Pallet)
                {
                    PalletList[i] = new KeyValuePair<Pallet, bool>(PalletList[i].Key, IsAvailable);
                    break;
                }
            }
        }
    }

    public class PalletRequest
    {
        public SKUSupply SKUSupply { get; private set; }
        public int RequiredLayerCount { get; private set; }
        public PalletRequestStatus Status { get; private set; }

        public PalletRequest(List<SKUSupply> SKUSupplyList, string ItemCode, int RequiredLayerCount)
        {
            this.RequiredLayerCount = RequiredLayerCount;
            this.SKUSupply = SKUSupplyList.Find(a => a.SKUData.ItemCode == ItemCode);
            this.Status = PalletRequestStatus.InQueue;
        }
        public List<Pallet> RequestPallet()
        {
            this.Status = PalletRequestStatus.Requested;
            return SKUSupply.RequestLayer(RequiredLayerCount);
        }
    }

    public class CustomerOrder
    {

    }
}
