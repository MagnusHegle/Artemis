﻿using System.Collections.ObjectModel;
using System.Reactive.Disposables;
using Artemis.Core;
using Artemis.Core.Modules;
using Artemis.Core.Services;
using Artemis.Storage.Entities.Profile;
using Artemis.UI.Shared.Services.NodeEditor;
using Artemis.UI.Shared.Services.NodeEditor.Commands;
using Artemis.UI.Shared.VisualScripting;
using ReactiveUI;

namespace Artemis.VisualScripting.Nodes.DataModel.Screens;

public class DataModelEventCycleNodeCustomViewModel : CustomNodeViewModel
{
    private readonly DataModelEventCycleNode _cycleNode;
    private readonly INodeEditorService _nodeEditorService;
    private DataModelPath? _dataModelPath;
    private ObservableCollection<Module>? _modules;
    private bool _updating;

    public DataModelEventCycleNodeCustomViewModel(DataModelEventCycleNode cycleNode, INodeScript script, ISettingsService settingsService, INodeEditorService nodeEditorService) : base(cycleNode, script)
    {
        _cycleNode = cycleNode;
        _nodeEditorService = nodeEditorService;

        ShowFullPaths = settingsService.GetSetting("ProfileEditor.ShowFullPaths", true);
        ShowDataModelValues = settingsService.GetSetting("ProfileEditor.ShowDataModelValues", false);
        Modules = new ObservableCollection<Module>();

        this.WhenActivated(d =>
        {
            // Set up extra modules
            if (_cycleNode.Script?.Context is Profile scriptProfile && scriptProfile.Configuration.Module != null)
                Modules = new ObservableCollection<Module> {scriptProfile.Configuration.Module};
            else if (_cycleNode.Script?.Context is ProfileConfiguration profileConfiguration && profileConfiguration.Module != null)
                Modules = new ObservableCollection<Module> {profileConfiguration.Module};

            // Subscribe to node changes 
            _cycleNode.WhenAnyValue(n => n.Storage).Subscribe(UpdateDataModelPath).DisposeWith(d);
            this.WhenAnyValue(vm => vm.DataModelPath).WhereNotNull().Subscribe(ApplyDataModelPath).DisposeWith(d);

            Disposable.Create(() =>
            {
                _dataModelPath?.Dispose();
                _dataModelPath = null;
            }).DisposeWith(d);
        });
    }

    public PluginSetting<bool> ShowFullPaths { get; }
    public PluginSetting<bool> ShowDataModelValues { get; }

    public ObservableCollection<Module>? Modules
    {
        get => _modules;
        set => this.RaiseAndSetIfChanged(ref _modules, value);
    }

    public DataModelPath? DataModelPath
    {
        get => _dataModelPath;
        set => this.RaiseAndSetIfChanged(ref _dataModelPath, value);
    }

    private void UpdateDataModelPath(DataModelPathEntity? entity)
    {
        try
        {
            if (_updating)
                return;

            _updating = true;

            DataModelPath? old = DataModelPath;
            DataModelPath = entity != null ? new DataModelPath(entity) : null;
            old?.Dispose();
        }
        finally
        {
            _updating = false;
        }
    }

    private void ApplyDataModelPath(DataModelPath path)
    {
        try
        {
            if (_updating)
                return;
            if (path.Path == _cycleNode.Storage?.Path)
                return;

            _updating = true;

            path.Save();
            _nodeEditorService.ExecuteCommand(Script, new UpdateStorage<DataModelPathEntity>(_cycleNode, path?.Entity, "event"));
        }
        finally
        {
            _updating = false;
        }
    }
}