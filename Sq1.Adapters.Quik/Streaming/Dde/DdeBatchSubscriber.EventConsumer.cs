﻿using System;
using System.Collections.Generic;

using Sq1.Core;

using Sq1.Adapters.Quik.Streaming.Dde.XlDde;
using Sq1.Adapters.Quik.Streaming.Monitor;

namespace Sq1.Adapters.Quik.Streaming.Dde {
	public partial class DdeBatchSubscriber {

		void level2_OnDataStructuresParsed_Table_butAlwaysOneElementInList(
							object sender, XlDdeTableMonitoringEventArg<List<LevelTwoOlv>> alwaysJustOneDom) {
			string msig = " //level2_OnDataStructuresParsed_Table_butAlwaysOneElementInList(" + sender + ")";
			XlDdeTableMonitoreable<LevelTwoOlv> tableLevel2 = sender as XlDdeTableMonitoreable<LevelTwoOlv>;
			if (tableLevel2 == null) {
				string msg = "I_MUST_HAVE_BEEN_INVOKED_WITH_XlDdeTableMonitoreable<Level2>";
				Assembler.PopupException(msg + msig);
				return;
			}
			QuikStreamingMonitorDomUserControl domResizeable = tableLevel2.WhereIamMonitored as QuikStreamingMonitorDomUserControl;
			if (domResizeable == null) {
				string msg = "I_MUST_HAVE_BEEN_QuikStreamingMonitorDomUserControl_tableLevel2.WhereIamMonitored[" + tableLevel2.WhereIamMonitored + "]";
				Assembler.PopupException(msg + msig);
				return;
			}
			// second BeginInvoke below is hell of overhead, but this one is light, and succeeds if the second fails => visible counters increase
			domResizeable.PopulateLevel2ToTitle();

			if (alwaysJustOneDom == null) {
				string msg = "MUST_NOT_BE_NULL_EVENT_ARG alwaysJustOneDom[" + alwaysJustOneDom + "]";
				Assembler.PopupException(msg + msig);
				return;
			}
			if (alwaysJustOneDom.DataStructureParsed == null) {
				string msg = "MUST_NOT_BE_NULL_PARSED alwaysJustOneDom.DataStructureParsed[" + alwaysJustOneDom.DataStructureParsed + "]";
				Assembler.PopupException(msg + msig);
				return;
			}
			if (alwaysJustOneDom.DataStructureParsed.Count != 1) {
				string msg = "MUST_BE_ONLY_ONE_LEVEL2_IN_THE_LIST alwaysJustOneDom.DataStructureParsed.Count[" + alwaysJustOneDom.DataStructureParsed.Count + "]";
				Assembler.PopupException(msg + msig);
				return;
			}
			LevelTwoOlv level2fromDde_pushTo_domResizeableUserControl = alwaysJustOneDom.DataStructureParsed[0];
			domResizeable.PopulateLevel2ToDomControl(level2fromDde_pushTo_domResizeableUserControl);
		}
	}
}