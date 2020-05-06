using System;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;

namespace dominions.characters
{
    public class GuiDialogSkin : GuiDialog
    {
        protected IInventory characterInv;
        protected ElementBounds insetSlotBounds;
        protected float yaw = -GameMath.PIHALF + 0.3f;
        protected bool rotateCharacter;
        protected bool didSave = false;

        ClientSkinNetwork clientSkinNetwork;

        public GuiDialogSkin(ICoreClientAPI capi, ClientSkinNetwork clientSkinNetwork) : base(capi)
        {
            this.clientSkinNetwork = clientSkinNetwork;
        }

        protected virtual void ComposeGuis()
        {
            double pad = GuiElementItemSlotGrid.unscaledSlotPadding;
            double slotsize = GuiElementPassiveItemSlot.unscaledSlotSize;

            ElementBounds leftPrevButtonBounds = ElementBounds.Fixed(75, 120 + pad + 52, 19, 19).WithFixedPadding(2);
            ElementBounds leftNextButtonBounds = ElementBounds.Fixed(75, 120 + pad + 52, 19, 19).WithFixedPadding(2).FixedRightOf(leftPrevButtonBounds, 6);
            ElementBounds textBounds = ElementBounds.Fixed(0, 120 + pad + 52, 19, 19).WithFixedPadding(2);

            ElementBounds humanButtonBounds = ElementBounds.Fixed(0, pad + 52, 19, 19).WithFixedPadding(2);
            ElementBounds dwarfButtonBounds = ElementBounds.Fixed(0, pad + 52, 19, 19).WithFixedPadding(2).FixedRightOf(humanButtonBounds, 40);

            ElementBounds maleButtonBounds = ElementBounds.Fixed(0, 57 + pad + 52, 19, 19).WithFixedPadding(2);
            ElementBounds femaleButtonBounds = ElementBounds.Fixed(0, 57 + pad + 52, 19, 19).WithFixedPadding(2).FixedRightOf(maleButtonBounds, 30);

            ElementBounds titleTextBounds = ElementBounds.Fixed(0, pad + 20, 19, 19).WithFixedPadding(2);

            characterInv = capi.World.Player.InventoryManager.GetOwnInventory(GlobalConstants.characterInvClassName);
            ElementBounds leftSlotBounds = ElementStdBounds.SlotGrid(EnumDialogArea.None, pad, 20 + pad, 1, 6).FixedGrow(2 * pad, 2 * pad);

            insetSlotBounds = ElementBounds.Fixed(pad + 65 + 65, pad + 20 + 2, 300 - 60, leftSlotBounds.fixedHeight - 2 * pad - 4);

            ElementBounds bgBounds = ElementBounds.Fill.WithFixedPadding(GuiStyle.ElementToDialogPadding);
            bgBounds.BothSizing = ElementSizing.FitToChildren;


            ElementBounds dialogBounds = ElementStdBounds
                .AutosizedMainDialog.WithAlignment(EnumDialogArea.None)
                .WithFixedAlignmentOffset(GuiStyle.DialogToScreenPadding, 0);


            ClearComposers();
            SingleComposer = capi.Gui
                .CreateCompo("playercharacter", dialogBounds)
                .AddDialogBG(bgBounds, true)
                //.AddDialogTitleBar(capi.World.Player.PlayerName, OnTitleBarClose)
                .BeginChildElements(bgBounds)

                    // Race
                    .AddStaticTextAutoBoxSize("Race:", CairoFont.WhiteSmallishText(), EnumTextOrientation.Right, titleTextBounds.FlatCopy())
                    .AddSmallButton("Human", () => ButtonClickRace("human"), humanButtonBounds.FlatCopy())
                    .AddSmallButton("Dwarf", () => ButtonClickRace("dwarf"), dwarfButtonBounds.FlatCopy())

                    // Sex
                    .AddStaticTextAutoBoxSize("Sex:", CairoFont.WhiteSmallishText(), EnumTextOrientation.Right, titleTextBounds = titleTextBounds.BelowCopy(0, 35))
                    .AddSmallButton("Male", () => ButtonClickSex("male"), maleButtonBounds.FlatCopy())
                    .AddSmallButton("Female", () => ButtonClickSex("female"), femaleButtonBounds.FlatCopy())

                    .AddStaticTextAutoBoxSize("Appearance:", CairoFont.WhiteSmallishText(), EnumTextOrientation.Right, titleTextBounds = titleTextBounds.BelowCopy(0, 35))

                    // Skin Color
                    .AddIconButton("left", (on) => OnPrevious("skincolor"), leftPrevButtonBounds.FlatCopy())
                    .AddIconButton("right", (on) => OnNext("skincolor"), leftNextButtonBounds.FlatCopy())
                    .AddStaticTextAutoBoxSize("Skin color:", CairoFont.WhiteDetailText(), EnumTextOrientation.Right, textBounds.FlatCopy())

                    // Eye Color
                    .AddIconButton("left", (on) => OnPrevious("eyecolor"), leftPrevButtonBounds = leftPrevButtonBounds.BelowCopy(0, 5))
                    .AddIconButton("right", (on) => OnNext("eyecolor"), leftNextButtonBounds = leftNextButtonBounds.BelowCopy(0, 5))
                    .AddStaticTextAutoBoxSize("Eye color:", CairoFont.WhiteDetailText(), EnumTextOrientation.Right, textBounds = textBounds.BelowCopy(0, 5))

                    // Hair Color
                    .AddIconButton("left", (on) => OnPrevious("haircolor"), leftPrevButtonBounds = leftPrevButtonBounds.BelowCopy(0, 5))
                    .AddIconButton("right", (on) => OnNext("haircolor"), leftNextButtonBounds = leftNextButtonBounds.BelowCopy(0, 5))
                    .AddStaticTextAutoBoxSize("Hair color:", CairoFont.WhiteDetailText(), EnumTextOrientation.Right, textBounds = textBounds.BelowCopy(0, 7))

                    // Hair Type
                    .AddIconButton("left", (on) => OnPrevious("hairtype"), leftPrevButtonBounds = leftPrevButtonBounds.BelowCopy(0, 5))
                    .AddIconButton("right", (on) => OnNext("hairtype"), leftNextButtonBounds = leftNextButtonBounds.BelowCopy(0, 5))
                    .AddStaticTextAutoBoxSize("Hair type:", CairoFont.WhiteDetailText(), EnumTextOrientation.Right, textBounds = textBounds.BelowCopy(0, 7))

                    // Facial Hair
                    .AddIconButton("left", (on) => OnPrevious("facialhair"), leftPrevButtonBounds = leftPrevButtonBounds.BelowCopy(0, 5))
                    .AddIconButton("right", (on) => OnNext("facialhair"), leftNextButtonBounds = leftNextButtonBounds.BelowCopy(0, 5))
                    .AddStaticTextAutoBoxSize("Facial hair:", CairoFont.WhiteDetailText(), EnumTextOrientation.Right, textBounds = textBounds.BelowCopy(0, 7))

                    .AddSmallButton("Confirm", () => TryClose(), textBounds = textBounds.BelowCopy(0, 7))

                    //.AddItemSlotGrid(characterInv, SendInvPacket, 1, new int[] { 0, 1, 2, 11, 3, 4 }, leftSlotBounds, "leftSlots")
                    .AddInset(insetSlotBounds, 2)
                //.AddItemSlotGrid(characterInv, SendInvPacket, 1, new int[] { 6, 7, 8, 10, 5, 9 }, rightSlotBounds, "rightSlots")
                .EndChildElements()
                .Compose()
            ;
        }



        public override void OnMouseDown(MouseEvent args)
        {
            base.OnMouseDown(args);

            rotateCharacter = insetSlotBounds.PointInside(args.X, args.Y);
        }

        public override void OnMouseUp(MouseEvent args)
        {
            base.OnMouseUp(args);

            rotateCharacter = false;
        }

        public override void OnMouseMove(MouseEvent args)
        {
            base.OnMouseMove(args);

            if (rotateCharacter) yaw -= args.DeltaX / 100f;
        }

        public override void OnRenderGUI(float deltaTime)
        {
            base.OnRenderGUI(deltaTime);

            capi.Render.GlPushMatrix();

            if (focused) { capi.Render.GlTranslate(0, 0, 150); }

            double pad = GuiElement.scaled(GuiElementItemSlotGridBase.unscaledSlotPadding);

            capi.Render.RenderEntityToGui(
                deltaTime,
                capi.World.Player.Entity,
                insetSlotBounds.renderX + pad - 20,
                insetSlotBounds.renderY + pad,
                120,
                yaw,
                (float)GuiElement.scaled(140),
                ColorUtil.WhiteArgb);


            capi.Render.GlPopMatrix();
        }

        public override void OnGuiOpened()
        {
            ComposeGuis();

            if (capi.World.Player.WorldData.CurrentGameMode == EnumGameMode.Guest || capi.World.Player.WorldData.CurrentGameMode == EnumGameMode.Survival)
            {
                if (characterInv != null) characterInv.Open(capi.World.Player);
            }
        }


        public override void OnGuiClosed()
        {
            if (characterInv != null)
            {
                characterInv.Close(capi.World.Player);
            }

        }

        protected virtual void OnTitleBarClose()
        {
            this.didSave = true;
            TryClose();
        }

        // May need this
        protected void SendInvPacket(object packet)
        {
            capi.Network.SendPacketClient(packet);
        }
        //

        public override string ToggleKeyCombinationCode
        {
            get { return "skindialog"; }
        }

        private void OnNext(string skinPart)
        {
            string currentPart = capi.World.Player.Entity.WatchedAttributes.GetString(skinPart);
            string nextPart;

            string[] skinOptions = Core.skinTypes[skinPart];
            int nextIndex = Array.IndexOf(skinOptions, currentPart) + 1;

            if (nextIndex < skinOptions.Length)
            {
                nextPart = skinOptions[nextIndex];
            }
            else
            {
                nextPart = skinOptions[0];
            }

            this.clientSkinNetwork.SendSkinPacket(skinPart, nextPart);
        }

        private void OnPrevious(string skinPart)
        {
            string currentPart = capi.World.Player.Entity.WatchedAttributes.GetString(skinPart);
            string previousPart;

            string[] skinOptions = Core.skinTypes[skinPart];
            int index = Array.IndexOf(skinOptions, currentPart);
            if (index > 0)
            {
                previousPart = skinOptions[index - 1];
            }
            else
            {
                previousPart = skinOptions[skinOptions.Length - 1];
            }

            this.clientSkinNetwork.SendSkinPacket(skinPart, previousPart);
        }

        private bool ButtonClickRace(string race)
        {
            this.clientSkinNetwork.SendSkinPacket("race", race);
            return true;
        }

        private bool ButtonClickSex(string sex)
        {
            this.clientSkinNetwork.SendSkinPacket("sex", sex);
            return true;
        }
    }
}
