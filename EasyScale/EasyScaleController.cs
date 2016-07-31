using System;
using System.Collections.Generic;
using spaar.ModLoader;
using UnityEngine;

namespace Lench.EasyScale
{
    public class EasyScaleController : SingleInstance<EasyScaleController>
    {
        public override string Name { get; } = "Easy Scale Controller";

        internal List<Guid> LoadedCylinderFix = new List<Guid>();

        private bool MachineLoaded = false;
        private bool MovingAllSliders = false;

        internal void OnMachineSave(MachineInfo info)
        {
            foreach (var blockinfo in info.Blocks.FindAll(b => b.ID == (int)BlockType.Brace))
            {
                var block = Machine.Active().BuildingBlocks.Find(b => b.Guid == blockinfo.Guid);
                blockinfo.BlockData.Write("bmt-length-fix", (block.Toggles.Find(toggle => toggle.Key == "length-fix").IsActive));
            }
        }

        internal void OnMachineLoad(MachineInfo info)
        {
            LoadedCylinderFix = new List<Guid>();
            foreach (var blockinfo in info.Blocks.FindAll(b => b.ID == (int)BlockType.Brace))
            {
                if (blockinfo.BlockData.HasKey("bmt-length-fix") &&
                    blockinfo.BlockData.ReadBool("bmt-length-fix"))
                    LoadedCylinderFix.Add(blockinfo.Guid);
            }

            MachineLoaded = true;
        }

        private void Update()
        {
            if (MachineLoaded)
            {
                EasyScale.AddAllSliders();
                EasyScale.FixAllCylinders();
                MachineLoaded = false;
            }

            if (BlockMapper.CurrentInstance != null)
            {
                if (MovingAllSliders && !Keybindings.Get(EasyScale.MoveAllSliderBinding).IsDown())
                    BlockMapper.CurrentInstance.Refresh();

                MovingAllSliders = Keybindings.Get(EasyScale.MoveAllSliderBinding).IsDown();

                if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.LeftCommand))
                {
                    if (Input.GetKey(KeyCode.C))
                        EasyScale.Copy(); 
                    if (Input.GetKey(KeyCode.V))
                        EasyScale.Paste();
                } 
            }
        }
    }
}
