﻿/*
 * The MIT License (MIT)
 * Copyright (c) StarX 2015 
 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using CrazyStorm.Core;

namespace CrazyStorm
{
    public partial class Main
    {
        #region Private Members
        ParticleSystem selectedParticle;
        DependencyObject aimRect;
        Component aimComponent;
        DependencyObject selectionRect;
        int selectionRectX, selectionRectY;
        bool selectingComponent;
        readonly List<Component> selectedComponents = new List<Component>();
        readonly DoubleClickDetector dclickDetector = new DoubleClickDetector();
        #endregion

        #region Private Methods
        void UpdateScreen()
        {
            //Get component layer.
            Canvas canvas = null;
            foreach (TabItem item in ParticleTabControl.Items)
            {
                var content = item.Content as Canvas;
                if ((string)item.Header == selectedParticle.Name)
                {
                    canvas = VisualHelper.VisualDownwardSearch(content, "ComponentLayer") as Canvas;
                    break;
                }
            }
            if (canvas != null)
            {
                //Update components on current screen.
                selectedComponents.Clear();
                canvas.Children.Clear();
                var itemTemplate = FindResource("ComponentItem") as DataTemplate;
                foreach (var layer in selectedParticle.Layers)
                    if (layer.Visible)
                        foreach (var component in layer.Components)
                        {
                            var item = itemTemplate.LoadContent() as Canvas;
                            var frame = VisualHelper.VisualDownwardSearch(item, "Frame") as Label;
                            var icon = VisualHelper.VisualDownwardSearch(item, "Icon") as Image;
                            var box = VisualHelper.VisualDownwardSearch(item, "Box") as Image;
                            frame.DataContext = layer;
                            icon.DataContext = component;
                            box.Opacity = component.Selected ? 1 : 0;
                            if (component.Selected)
                                selectedComponents.Add(component);
                            for (int i = 0; i < Component.ComponentNames.Count; ++i)
                                if (Component.ComponentNames[i] == component.GetType().Name)
                                {
                                    icon.Source = new BitmapImage(new Uri(@"Images/button" + (i+1) + ".png", UriKind.Relative));
                                    break;
                                }

                            item.SetValue(Canvas.LeftProperty, (double)component.X - box.Width / 2 + config.ScreenWidthOver2);
                            item.SetValue(Canvas.TopProperty, (double)component.Y - box.Height / 2 + config.ScreenHeightOver2);
                            canvas.Children.Add(item as UIElement);
                        }
            }
        }
        void SelectComponents(int x, int y, int width, int height)
        {
            var set = new List<Component>();
            //Select those involved in selection rect. 
            int index = 0;
            foreach (var layer in selectedParticle.Layers)
                if (layer.Visible)
                    foreach (var component in layer.Components)
                    {
                        var selectRect = new Rect(x, y, width, height);
                        var componentRect = new Rect(component.X - config.GridWidth / 2 + config.ScreenWidthOver2,
                            component.Y - config.GridHeight / 2 + config.ScreenHeightOver2, config.GridWidth, config.GridHeight);
                        if (selectRect.IntersectsWith(componentRect))
                        {
                            //Prevent overlay shade from preceding components.
                            if (width == 0 && height == 0 && set.Count > 0)
                                set[set.Count - 1] = component;
                            else
                                set.Add(component);
                        }
                        index++;
                    }
            SelectComponents(set, width == 0 && height == 0);
        }
        void SelectComponents(List<Component> set, bool canDoubleClick)
        {
            foreach (var layer in selectedParticle.Layers)
                if (layer.Visible)
                    foreach (var component in layer.Components)
                    {
                        component.Selected = false;
                        if (set != null)
                        {
                            foreach (var target in set)
                                if (component == target)
                                {
                                    component.Selected = true;
                                    break;
                                }
                        }
                    }

            Update();
            //Enable delection if selected one or more.
            DeleteComponentItem.IsEnabled = selectedComponents.Count > 0;
            //Enable binding if selected one.
            BindComponentItem.IsEnabled = selectedComponents.Count == 1;
            UnbindComponentItem.IsEnabled = BindComponentItem.IsEnabled;
            //Check double click.
            if (!canDoubleClick || set == null)
                return;

            dclickDetector.Start();
            if (set.Count > 0 && dclickDetector.IsDetected())
                CreatePropertyPanel(set.First());
        }
        void AddNewParticleTab(ParticleSystem particle)
        {
            var tabItem = new TabItem();
            tabItem.Header = particle.Name;
            tabItem.Content = ParticleTabControl.ItemTemplate.LoadContent() as Canvas;
            ParticleTabControl.Items.Add(tabItem);
            ParticleTabControl.SelectedItem = tabItem;
        }
        #endregion

        #region Window EventHandler
        private void Screen_MouseMove(object sender, MouseEventArgs e)
        {
            Point point = e.GetPosition(sender as IInputElement);
            int x = (int)point.X;
            int y = (int)point.Y;
            if (x >= 0 && x < config.ScreenWidth && y >= 0 && y < config.ScreenHeight)
            {
                //Display a rect with red edge to mark the location that component will be put on.
                if (aimRect != null)
                {
                    if (config.GridAlignment)
                    {
                        aimRect.SetValue(Canvas.LeftProperty, (double)((x / (int)config.GridWidth) * config.GridWidth));
                        aimRect.SetValue(Canvas.TopProperty, (double)((y / (int)config.GridHeight) * config.GridHeight));
                    }
                    else if (x <= config.ScreenWidth - config.GridWidth && y <= config.ScreenHeight - config.GridHeight)
                    {
                        aimRect.SetValue(Canvas.LeftProperty, (double)x);
                        aimRect.SetValue(Canvas.TopProperty, (double)y);
                    }
                }
                //Display a coloured  rect to mark the range that is selecting.
                if (selectionRect != null)
                {
                    var width = x - selectionRectX;
                    if (width >= 0)
                    {
                        selectionRect.SetValue(Canvas.LeftProperty, (double)selectionRectX);
                        selectionRect.SetValue(WidthProperty, (double)width);
                    }
                    else
                    {
                        selectionRect.SetValue(Canvas.LeftProperty, (double)x);
                        selectionRect.SetValue(WidthProperty, (double)-width);
                    }
                    var height = y - selectionRectY;
                    if (height >= 0)
                    {
                        selectionRect.SetValue(Canvas.TopProperty, (double)selectionRectY);
                        selectionRect.SetValue(HeightProperty, (double)height);
                    }
                    else
                    {
                        selectionRect.SetValue(Canvas.TopProperty, (double)y);
                        selectionRect.SetValue(HeightProperty, (double)-height);
                    }
                }
            }
        }
        private void Screen_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            //Take away the rect.
            if (aimRect != null)
            {
                aimRect.SetValue(OpacityProperty, 0.0d);
                aimRect = null;
            }
        }
        private void Screen_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            //Show selection rect.
            Point point = e.GetPosition(sender as IInputElement);
            int x = (int)point.X;
            int y = (int)point.Y;
            selectingComponent = true;
            var content = (DependencyObject)ParticleTabControl.SelectedContent;
            selectionRect = VisualHelper.VisualDownwardSearch(content, "SelectingBox");
            selectionRect.SetValue(Canvas.LeftProperty, (double)x);
            selectionRect.SetValue(Canvas.TopProperty, (double)y);
            selectionRectX = x;
            selectionRectY = y;
            //When aimed then add component to the place mouse down with left-button.
            if (aimRect != null)
            {
                aimRect.SetValue(OpacityProperty, 0.0d);
                var boxX = (double)aimRect.GetValue(Canvas.LeftProperty);
                var boxY = (double)aimRect.GetValue(Canvas.TopProperty);
                aimComponent.X = (int)(boxX + (double)aimRect.GetValue(Canvas.WidthProperty) / 2 - config.ScreenWidthOver2);
                aimComponent.Y = (int)(boxY + (double)aimRect.GetValue(Canvas.HeightProperty) / 2 - config.ScreenHeightOver2);
                new AddComponentCommand().Do(commandStacks[selectedParticle],
                    selectedParticle, selectedLayer, aimComponent);
                Update();
                aimComponent = null;
                aimRect = null;
            }
        }
        private void ParticleTabControl_MouseLeave(object sender, MouseEventArgs e)
        {
            //Cancel selection when mouse leaves.
            if (selectingComponent)
                ParticleTabControl_MouseLeftButtonUp(sender, null);
        }
        private void ParticleTabControl_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            //Determine selected components.
            selectingComponent = false;
            if (selectionRect != null)
            {
                var x = (double)selectionRect.GetValue(LeftProperty);
                var y = (double)selectionRect.GetValue(TopProperty);
                var width = (double)selectionRect.GetValue(WidthProperty);
                var height = (double)selectionRect.GetValue(HeightProperty);
                SelectComponents((int)x, (int)y, (int)width, (int)height);
                selectionRect.SetValue(WidthProperty, 0.0d);
                selectionRect.SetValue(HeightProperty, 0.0d);
                selectionRect = null;
            }
            else
                SelectComponents(null, true);
        }
        private void ParticleTabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            //Maintain selected Particle.
            if (e.AddedItems.Count > 0)
            {
                var tabItem = e.AddedItems[0] as TabItem;
                foreach (var item in file.Particles)
                    if (item.Name == (string)tabItem.Header)
                    {
                        selectedParticle = item;
                        break;
                    }
                Update();
            }
        }
        private void ScreenSettingItem_Click(object sender, RoutedEventArgs e)
        {
            //Open screen setting window.
            ScreenSetting window = new ScreenSetting(config);
            window.Owner = this;
            window.ShowDialog();
        }
        #endregion
    }
}
