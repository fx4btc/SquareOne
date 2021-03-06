using System;

using Sq1.Core.DataTypes;

namespace Sq1.Core.Charting {
	public partial class ChartShadow {
		public event EventHandler<EventArgs>	OnChartSettingsChanged_containerShouldSerialize;
		public event EventHandler<EventArgs>	OnContextScriptChanged_containerShouldSerialize;

		public event EventHandler<BarEventArgs>	OnBarStreamingUpdatedMerged;

		public event EventHandler<EventArgs>	OnPumpPaused;
		public event EventHandler<EventArgs>	OnPumpUnPaused;

		void raiseOnBarStreamingUpdatedMerged(BarEventArgs e) {
			if (this.OnBarStreamingUpdatedMerged == null) return;
			try {
				this.OnBarStreamingUpdatedMerged(this, e);
			} catch (Exception ex) {
				string msg = "RaiseBarStreamingUpdatedMerged(bar[" + e.Bar + "])";
				Assembler.PopupException(msg, ex, false);
			}
		}
		
		public void RaiseOnChartSettingsChanged_containerShouldSerialize() {
			if (this.OnChartSettingsChanged_containerShouldSerialize == null) return;
			try {
				this.OnChartSettingsChanged_containerShouldSerialize(this, null);
			} catch (Exception ex) {
				Assembler.PopupException("RaiseChartSettingsChangedContainerShouldSerialize()", ex);
			}
		}
		public void RaiseOnContextScriptChanged_containerShouldSerialize() {
			if (this.OnContextScriptChanged_containerShouldSerialize == null) return;
			try {
				this.OnContextScriptChanged_containerShouldSerialize(this, null);
			} catch (Exception ex) {
				Assembler.PopupException("RaiseContextScriptChangedContainerShouldSerialize()", ex);
			}
		}

		void raiseOnPumpPaused() {
			if (this.OnPumpPaused == null) return;
			try {
				this.OnPumpPaused(this, null);
			} catch (Exception ex) {
				string msg = "RaiseOnPumpPaused()";
				Assembler.PopupException(msg, ex, false);
			}
		}

		void raiseOnPumpUnPaused() {
			if (this.OnPumpUnPaused == null) return;
			try {
				this.OnPumpUnPaused(this, null);
			} catch (Exception ex) {
				string msg = "RaiseOnPumpUnPaused()";
				Assembler.PopupException(msg, ex, false);
			}
		}

	}
}
