﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Text;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using MaterialSkin.Animations;

namespace MaterialSkin.Controls
{
    public class MaterialTabSelector : Control, IMaterialControl
    {
        public int Depth { get; set; }
        public MaterialSkinManager SkinManager { get { return MaterialSkinManager.Instance; } }
        public MouseState MouseState { get; set; }

        private MaterialTabControl baseTabControl;
        public MaterialTabControl BaseTabControl
        {
            get { return baseTabControl; }
            set
            {
                baseTabControl = value;
                if (baseTabControl == null) return;
                previousSelectedTabIndex = baseTabControl.SelectedIndex;
                baseTabControl.Deselected += (sender, args) =>
                {
                    previousSelectedTabIndex = baseTabControl.SelectedIndex;
                };
                baseTabControl.SelectedIndexChanged += (sender, args) =>
                {
                    animationManager.SetProgress(0);
                    animationManager.StartNewAnimation(AnimationDirection.In);
                };
                baseTabControl.ControlAdded += delegate
                {
                    Invalidate();
                };
                baseTabControl.ControlRemoved += delegate
                {
                    Invalidate();
                };
            }
        }

        private int previousSelectedTabIndex;
        private Point animationSource;
        private readonly AnimationManager animationManager;

        private List<Rectangle> TabRects;
        private const int TAB_HEADER_PADDING = 24;
        private const int TAB_INDICATOR_HEIGHT = 2;

        public MaterialTabSelector()
        {
            SetStyle(ControlStyles.DoubleBuffer | ControlStyles.OptimizedDoubleBuffer, true);
            Height = 48;

            animationManager = new AnimationManager()
            {
                AnimationType = AnimationType.EaseInOut,
                Increment = 0.04,
            };
            animationManager.OnAnimationProgress += sender => Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.TextRenderingHint = TextRenderingHint.AntiAlias;

            g.Clear(SkinManager.PrimaryColor);

            if (baseTabControl == null) return;

            if (!animationManager.IsAnimating() || TabRects == null)
                UpdateTabRects();

            double animationProgress = animationManager.GetProgress();

            //Click feedback
            if (animationManager.IsAnimating())
            {
                var rippleBrush = new SolidBrush(Color.FromArgb((int)(51 - (animationProgress * 50)), Color.White));
                var rippleSize = (int)(animationProgress * TabRects[baseTabControl.SelectedIndex].Width * 1.75);

                g.SetClip(TabRects[baseTabControl.SelectedIndex]);
                g.FillEllipse(rippleBrush, new Rectangle(animationSource.X - rippleSize / 2, animationSource.Y - rippleSize / 2, rippleSize, rippleSize));
                g.ResetClip();
                rippleBrush.Dispose();
            }

            //Draw tab headers
            foreach (TabPage tabPage in baseTabControl.TabPages)
            {
                int currentTabIndex = tabPage.TabIndex;
                Brush textBrush = new SolidBrush(Color.FromArgb(CalculateTextAlpha(currentTabIndex, animationProgress), SkinManager.ACTION_BAR_TEXT));

                g.DrawString(tabPage.Text.ToUpper(), SkinManager.ROBOTO_MEDIUM_10, textBrush, TabRects[currentTabIndex], new StringFormat() { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center });
                textBrush.Dispose();
            }

            //Animate tab indicator
            int previousSelectedTabIndexIfHasOne = previousSelectedTabIndex == -1 ? baseTabControl.SelectedIndex : previousSelectedTabIndex;
            Rectangle previousActiveTabRect = TabRects[previousSelectedTabIndexIfHasOne];
            Rectangle activeTabPageRect = TabRects[baseTabControl.SelectedIndex];

            int y = activeTabPageRect.Bottom - 2;
            int x = previousActiveTabRect.X + (int)((activeTabPageRect.X - previousActiveTabRect.X) * animationProgress);
            int width = previousActiveTabRect.Width + (int)((activeTabPageRect.Width - previousActiveTabRect.Width) * animationProgress);

            g.FillRectangle(SkinManager.AccentColorBrush, x, y, width, TAB_INDICATOR_HEIGHT);
        }

        private int CalculateTextAlpha(int tabIndex, double animationProgress)
        {
            int primaryA = SkinManager.ACTION_BAR_TEXT.A;
            int secondaryA = SkinManager.ACTION_BAR_TEXT_SECONDARY.A;

            if (tabIndex == baseTabControl.SelectedIndex && !animationManager.IsAnimating())
            {
                return primaryA;
            }
            if (tabIndex != previousSelectedTabIndex && tabIndex != baseTabControl.SelectedIndex)
            {
                return secondaryA;
            }
            if (tabIndex == previousSelectedTabIndex)
            {
                return primaryA - (int) ((primaryA - secondaryA)* animationProgress);
            }
            return secondaryA + (int)((primaryA - secondaryA) * animationProgress);
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);

            if (TabRects == null) UpdateTabRects();
            for (int i = 0; i < TabRects.Count; i++)
            {
                if (TabRects[i].Contains(e.Location))
                {
                    baseTabControl.SelectedIndex = i;
                }
            }

            animationSource = e.Location;
        }

        private void UpdateTabRects()
        {
            TabRects = new List<Rectangle>();

            //If there isn't a base tab control, the rects shouldn't be calculated
            //If there aren't tab pages in the base tab control, the list should just be empty which has been set already; exit the void
            if (baseTabControl == null || baseTabControl.TabCount == 0) return;

            //Caluclate the bounds of each tab header specified in the base tab control
            using (var b = new Bitmap(1,1))
            {
                using (var g = Graphics.FromImage(b))
                {
                    TabRects.Add(new Rectangle(SkinManager.FORM_PADDING, 0, TAB_HEADER_PADDING * 2 + (int)g.MeasureString(baseTabControl.TabPages[0].Text, SkinManager.ROBOTO_MEDIUM_10).Width, Height));
                    for (int i = 1; i < baseTabControl.TabPages.Count; i++)
                    {
                        TabRects.Add(new Rectangle(TabRects[i - 1].Right, 0, TAB_HEADER_PADDING * 2 + (int)g.MeasureString(baseTabControl.TabPages[i].Text, SkinManager.ROBOTO_MEDIUM_10).Width, Height));
                    }
                }
            }
        }
    }
}
