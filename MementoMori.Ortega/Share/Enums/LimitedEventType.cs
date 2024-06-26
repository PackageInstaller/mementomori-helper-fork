﻿using System.ComponentModel;

namespace MementoMori.Ortega.Share.Enums
{
	[Description("期間限定イベントの種類")]
	public enum LimitedEventType
	{
		[Description("不明")]
		None,
		[Description("属性の塔全開放")]
		ElementTowerAllRelease,
		[Description("シリアルコード入力")]
		SerialCode
	}
}
