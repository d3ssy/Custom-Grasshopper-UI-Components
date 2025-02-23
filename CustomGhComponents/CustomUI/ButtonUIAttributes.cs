﻿using Grasshopper.Kernel.Attributes;
using Grasshopper.GUI.Canvas;
using Grasshopper.GUI;
using Grasshopper.Kernel;
using System;
using System.Drawing;
using System.Collections.Generic;
using Rhino.Display;
using System.Drawing.Drawing2D;

namespace CustomUI
{
    /// <summary>
    /// Class to create custom component UI with a button
    /// 
    /// To use this method override CreateAttributes() in component class and set m_attributes = new ButtonUIAttributes(...
    /// </summary>
    public class ButtonUIAttributes : GH_ComponentAttributes
    {
        public ButtonUIAttributes(GH_Component owner, string displayText, Action clickHandle, string spacerText = "") : base(owner) { buttonText = displayText; SpacerTxt = spacerText; action = clickHandle; }

        readonly string buttonText; // text to be displayed
        System.Drawing.RectangleF ButtonBounds; // area for button to be displayed
        readonly Action action;

        RectangleF SpacerBounds; // spacer between standard component and button
        readonly string SpacerTxt; // text to be displayed on spacer

        bool mouseDown;
        float MinWidth
        {
            get
            {
                List<string> spacers = new List<string>();
                spacers.Add(SpacerTxt);
                float sp = MaxTextWidth(spacers, GH_FontServer.Small);
                List<string> buttons = new List<string>();
                buttons.Add(buttonText);
                float bt = MaxTextWidth(buttons, GH_FontServer.Standard);

                float num = Math.Max(Math.Max(sp, bt), 90);
                return num;
            }
            set { MinWidth = value; }
        }
        protected override void Layout()
        {
            base.Layout();

            // first change the width to suit; using max to determine component visualisation style
            FixLayout();

            int s = 2; //spacing to edges and internal between boxes

            int h0 = 0;
            //spacer and title
            if (SpacerTxt != "")
            {
                h0 = 10;
                SpacerBounds = new RectangleF(Bounds.X, Bounds.Bottom + s / 2, Bounds.Width, h0);
            }

            int h1 = 20; // height of button
            // create text box placeholders
            ButtonBounds = new RectangleF(Bounds.X + 2 * s, Bounds.Bottom + h0 + 2 * s, Bounds.Width - 4 * s, h1);

            //update component bounds
            Bounds = new RectangleF(Bounds.X, Bounds.Y, Bounds.Width, Bounds.Height + h0 + h1 + 4 * s);
        }

        protected override void Render(GH_Canvas canvas, System.Drawing.Graphics graphics, GH_CanvasChannel channel)
        {
            base.Render(canvas, graphics, channel);

            if (channel == GH_CanvasChannel.Objects)
            {
                Pen spacer = new Pen(ButtonColours.SpacerColour);

                Font font = GH_FontServer.Standard;
                // adjust fontsize to high resolution displays
                font = new Font(font.FontFamily, font.Size / GH_GraphicsUtil.UiScale, FontStyle.Regular);

                Font sml = GH_FontServer.Small;
                // adjust fontsize to high resolution displays
                sml = new Font(sml.FontFamily, sml.Size / GH_GraphicsUtil.UiScale, FontStyle.Regular);

                //Draw divider line
                if (SpacerTxt != "")
                {
                    graphics.DrawString(SpacerTxt, sml, ButtonColours.AnnotationTextDark, SpacerBounds, GH_TextRenderingConstants.CenterCenter);
                    graphics.DrawLine(spacer, SpacerBounds.X, SpacerBounds.Y + SpacerBounds.Height / 2, SpacerBounds.X + (SpacerBounds.Width - GH_FontServer.StringWidth(SpacerTxt, sml)) / 2 - 4, SpacerBounds.Y + SpacerBounds.Height / 2);
                    graphics.DrawLine(spacer, SpacerBounds.X + (SpacerBounds.Width - GH_FontServer.StringWidth(SpacerTxt, sml)) / 2 + GH_FontServer.StringWidth(SpacerTxt, sml) + 4, SpacerBounds.Y + SpacerBounds.Height / 2, SpacerBounds.X + SpacerBounds.Width, SpacerBounds.Y + SpacerBounds.Height / 2);
                }

                // Draw button box
                System.Drawing.Drawing2D.GraphicsPath button = RoundedRect(ButtonBounds, 2);

                Brush normal_colour = ButtonColours.ButtonColor;
                Brush hover_colour = ButtonColours.HoverButtonColour;
                Brush clicked_colour = ButtonColours.ClickedButtonColour;

                Brush butCol = (mouseOver) ? hover_colour : normal_colour;
                graphics.FillPath(mouseDown ? clicked_colour : butCol, button);

                // draw button edge
                Color edgeColor = ButtonColours.BorderColour;
                Color edgeHover = ButtonColours.HoverBorderColour;
                Color edgeClick = ButtonColours.ClickedBorderColour;
                Color edgeCol = (mouseOver) ? edgeHover : edgeColor;
                Pen pen = new Pen(mouseDown ? edgeClick : edgeCol)
                {
                    Width = (mouseDown) ? 0.8f : 0.5f
                };
                graphics.DrawPath(pen, button);

                System.Drawing.Drawing2D.GraphicsPath overlay = RoundedRect(ButtonBounds, 2, true);
                graphics.FillPath(new SolidBrush(Color.FromArgb(mouseDown ? 0 : mouseOver ? 40 : 60, 255, 255, 255)), overlay);

                // draw button text
                graphics.DrawString(buttonText, font, ButtonColours.AnnotationTextBright, ButtonBounds, GH_TextRenderingConstants.CenterCenter);
            }
        }
        public override GH_ObjectResponse RespondToMouseDown(GH_Canvas sender, GH_CanvasMouseEvent e)
        {
            if (e.Button == System.Windows.Forms.MouseButtons.Left)
            {
                System.Drawing.RectangleF rec = ButtonBounds;
                if (rec.Contains(e.CanvasLocation))
                {
                    mouseDown = true;
                    Owner.OnDisplayExpired(false);
                    return GH_ObjectResponse.Capture;
                }
            }
            return base.RespondToMouseDown(sender, e);
        }

        public override GH_ObjectResponse RespondToMouseUp(GH_Canvas sender, GH_CanvasMouseEvent e)
        {
            if (e.Button == System.Windows.Forms.MouseButtons.Left)
            {
                System.Drawing.RectangleF rec = ButtonBounds;
                if (rec.Contains(e.CanvasLocation))
                {
                    if (mouseDown)
                    {
                        mouseDown = false;
                        mouseOver = false;
                        Owner.OnDisplayExpired(false);
                        action();
                        //                        Owner.ExpireSolution(true);
                        return GH_ObjectResponse.Release;
                    }
                }
            }
            return base.RespondToMouseUp(sender, e);
        }
        bool mouseOver;
        public override GH_ObjectResponse RespondToMouseMove(GH_Canvas sender, GH_CanvasMouseEvent e)
        {
            if (ButtonBounds.Contains(e.CanvasLocation))
            {
                mouseOver = true;
                Owner.OnDisplayExpired(false);
                sender.Cursor = System.Windows.Forms.Cursors.Hand;
                return GH_ObjectResponse.Capture;
            }

            if (mouseOver)
            {
                mouseOver = false;
                Owner.OnDisplayExpired(false);
                Grasshopper.Instances.CursorServer.ResetCursor(sender);
                return GH_ObjectResponse.Release;
            }

            return base.RespondToMouseMove(sender, e);
        }


        protected void FixLayout()
        {
            float width = this.Bounds.Width; // initial component width before UI overrides
            float num = Math.Max(width, MinWidth); // number for new width
            float num2 = 0f; // value for increased width (if any)

            // first check if original component must be widened
            if (num > width)
            {
                num2 = num - width; // change in width
                // update component bounds to new width
                this.Bounds = new RectangleF(
                    this.Bounds.X - num2 / 2f,
                    this.Bounds.Y,
                    num,
                    this.Bounds.Height);
            }

            // secondly update position of input and output parameter text
            // first find the maximum text width of parameters

            foreach (IGH_Param item in base.Owner.Params.Output)
            {
                PointF pivot = item.Attributes.Pivot; // original anchor location of output
                RectangleF bounds = item.Attributes.Bounds; // text box itself
                item.Attributes.Pivot = new PointF(
                    pivot.X + num2 / 2f, // move anchor to the right
                    pivot.Y);
                item.Attributes.Bounds = new RectangleF(
                    bounds.Location.X + num2 / 2f,  // move text box to the right
                    bounds.Location.Y,
                    bounds.Width,
                    bounds.Height);
            }
            // for input params first find the widest input text box as these are right-aligned
            float inputwidth = 0f;
            foreach (IGH_Param item in base.Owner.Params.Input)
            {
                if (inputwidth < item.Attributes.Bounds.Width)
                    inputwidth = item.Attributes.Bounds.Width;
            }
            foreach (IGH_Param item2 in base.Owner.Params.Input)
            {
                PointF pivot2 = item2.Attributes.Pivot; // original anchor location of input
                RectangleF bounds2 = item2.Attributes.Bounds;
                item2.Attributes.Pivot = new PointF(
                    pivot2.X - num2 / 2f + inputwidth, // move to the left, move back by max input width
                    pivot2.Y);
                item2.Attributes.Bounds = new RectangleF(
                     bounds2.Location.X - num2 / 2f,
                     bounds2.Location.Y,
                     bounds2.Width,
                     bounds2.Height);
            }
        }
        public static float MaxTextWidth(List<string> spacerTxts, Font font)
        {
            float sp = new float(); //width of spacer text

            // adjust fontsize to high resolution displays
            font = new Font(font.FontFamily, font.Size / GH_GraphicsUtil.UiScale, FontStyle.Regular);

            for (int i = 0; i < spacerTxts.Count; i++)
            {
                if (GH_FontServer.StringWidth(spacerTxts[i], font) + 8 > sp)
                    sp = GH_FontServer.StringWidth(spacerTxts[i], font) + 8;
            }
            return sp;
        }

        public static GraphicsPath RoundedRect(RectangleF bounds, int radius, bool overlay = false)
        {
            RectangleF b = new RectangleF(bounds.X, bounds.Y, bounds.Width, bounds.Height);
            int diameter = radius * 2;
            Size size = new Size(diameter, diameter);
            RectangleF arc = new RectangleF(b.Location, size);
            GraphicsPath path = new GraphicsPath();
            
            if (overlay)
                b.Height = diameter;

            if (radius == 0)
            {
                path.AddRectangle(b);
                return path;
            }

            // top left arc  
            path.AddArc(arc, 180, 90);

            // top right arc  
            arc.X = b.Right - diameter;
            path.AddArc(arc, 270, 90);

            if (!overlay)
            {
                // bottom right arc  
                arc.Y = b.Bottom - diameter;
                path.AddArc(arc, 0, 90);

                // bottom left arc 
                arc.X = b.Left;
                path.AddArc(arc, 90, 90);
            }
            else
            {
                path.AddLine(new PointF(b.X + b.Width, b.Y + b.Height), new PointF(b.X, b.Y + b.Height));
            }

            path.CloseFigure();
            return path;
        }
    }


    /// <summary>
    /// Colour class holding the main colours used in colour scheme. 
    /// Make calls to this class to be able to easy update colours.
    /// 
    /// </summary>
    public class ButtonColours
    {
        //Set colours for Component UI
        static readonly Color Primary = Color.FromArgb(255, 229, 27, 36);
        static readonly Color Primary_light = WhiteOverlay(Primary, 0.32);
        static readonly Color Primary_dark = Overlay(Primary, Color.Black, 0.32);
        public static Brush ButtonColor
        {
            get { return new SolidBrush(Primary); }
        }
        public static Brush ClickedButtonColor
        {
            get { return new SolidBrush(Primary_light); }
        }
        public static Color BorderColour
        {
            get { return Primary_dark; }
        }
        public static Color ClickedBorderColour
        {
            get { return Primary; }
        }
        public static Color SpacerColour
        {
            get { return Color.DarkGray; }
        }
        public static Brush AnnotationTextDark
        {
            get { return Brushes.Black; }
        }
        public static Brush AnnotationTextBright
        {
            get { return Brushes.White; }
        }
        public static Brush ClickedButtonColour
        {
            get { return new SolidBrush(WhiteOverlay(Primary, 0.32)); }
        }
        public static Brush HoverButtonColour
        {
            get { return new SolidBrush(Overlay(Primary, Color.Black, 0.04)); }
        }
        public static Color HoverBorderColour
        {
            get { return WhiteOverlay(Primary, 0.86); }
        }

        public static Color WhiteOverlay(Color original, double ratio)
        {
            Color white = Color.White;
            return Color.FromArgb(255,
                (int)(ratio * white.R + (1 - ratio) * original.R),
                (int)(ratio * white.G + (1 - ratio) * original.G),
                (int)(ratio * white.B + (1 - ratio) * original.B));
        }
        public static Color Overlay(Color original, Color overlay, double ratio)
        {
            return Color.FromArgb(255,
                (int)(ratio * overlay.R + (1 - ratio) * original.R),
                (int)(ratio * overlay.G + (1 - ratio) * original.G),
                (int)(ratio * overlay.B + (1 - ratio) * original.B));
        }

    }
}