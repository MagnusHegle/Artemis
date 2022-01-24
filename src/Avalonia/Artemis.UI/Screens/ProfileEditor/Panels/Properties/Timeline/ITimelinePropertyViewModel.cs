﻿using System;
using System.Collections.Generic;
using Artemis.UI.Screens.ProfileEditor.Properties.Timeline.Keyframes;
using ReactiveUI;

namespace Artemis.UI.Screens.ProfileEditor.Properties.Timeline;

public interface ITimelinePropertyViewModel : IReactiveObject
{
    List<ITimelineKeyframeViewModel> GetAllKeyframeViewModels();
    void WipeKeyframes(TimeSpan? start, TimeSpan? end);
    void ShiftKeyframes(TimeSpan? start, TimeSpan? end, TimeSpan amount);
}