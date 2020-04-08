﻿using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using Avalonia.Controls;
using Avalonia.Platform.Interop;

namespace Avalonia.Native.Interop
{
    public partial class IAvnAppMenu
    {
        private AvaloniaNativeMenuExporter _exporter;
        private NativeMenu _menu;
        private List<IAvnAppMenuItem> _menuItems = new List<IAvnAppMenuItem>();
        private Dictionary<NativeMenuItemBase, IAvnAppMenuItem> _menuItemLookup = new Dictionary<NativeMenuItemBase, IAvnAppMenuItem>();

        private void Remove(IAvnAppMenuItem item)
        {
            _menuItemLookup.Remove(item.Managed);
            _menuItems.Remove(item);            

            RemoveItem(item);
        }

        internal void Cleanup()
        {           
            foreach(var item in _menuItems)
            {
                item.Cleanup();
            }

            ((INotifyCollectionChanged)_menu.Items).CollectionChanged -= IAvnAppMenu_CollectionChanged;
            _exporter = null;
            _menu = null;
        }

        private void InsertAt(int index, IAvnAppMenuItem item)
        {
            if (item.Managed == null)
            {
                throw new InvalidOperationException("Cannot insert item that with Managed link null");
            }

            _menuItemLookup.Add(item.Managed, item);
            _menuItems.Insert(index, item);

            AddItem(item); // todo change to insertatimpl
        }

        private IAvnAppMenuItem CreateNew(IAvaloniaNativeFactory factory, NativeMenuItemBase item)
        {
            var nativeItem = item is NativeMenuItemSeperator ? factory.CreateMenuItemSeperator() : factory.CreateMenuItem();
            nativeItem.Managed = item;

            return nativeItem;
        }

        internal void Update(AvaloniaNativeMenuExporter exporter, IAvaloniaNativeFactory factory, NativeMenu menu, string title = "")
        {
            if (_menu == null)
            {
                _menu = menu;
            }
            else if (_menu != menu)
            {
                Cleanup();
                _menu = menu;
            }

            _exporter = exporter;

            ((INotifyCollectionChanged)_menu.Items).CollectionChanged += IAvnAppMenu_CollectionChanged;

            if (!string.IsNullOrWhiteSpace(title))
            {
                using (var buffer = new Utf8Buffer(title))
                {
                    Title = buffer.DangerousGetHandle();
                }
            }

            for (int i = 0; i < menu.Items.Count; i++)
            {
                IAvnAppMenuItem nativeItem = null;

                if (i >= _menuItems.Count || menu.Items[i] != _menuItems[i].Managed)
                {
                    if (_menuItemLookup.TryGetValue(menu.Items[i], out nativeItem))
                    {
                        Remove(nativeItem);
                        InsertAt(i, nativeItem);
                    }
                    else
                    {
                        nativeItem = CreateNew(factory, menu.Items[i]);
                        InsertAt(i, nativeItem);
                    }
                }

                if (menu.Items[i] is NativeMenuItem nmi)
                {
                    nativeItem.Update(exporter, factory, nmi);
                }
            }

            for (int i = menu.Items.Count; i < _menuItems.Count; i++)
            {
                Remove(_menuItems[i]);

                _menuItems[i].Cleanup();
            }
        }

        private void IAvnAppMenu_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            _exporter.QueueReset();
        }
    }
}
