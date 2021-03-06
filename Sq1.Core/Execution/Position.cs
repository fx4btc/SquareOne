using System;
using System.Text;
using System.Diagnostics;

using Sq1.Core.DataTypes;
using Sq1.Core.Backtesting;
using Sq1.Core.Livesim;

namespace Sq1.Core.Execution {
	public partial class Position : IDisposable {
		public int SernoAbs;
		
		public bool					IsLong					{ get { return this.PositionLongShort == PositionLongShort.Long; } }
		public bool					IsShort					{ get { return this.PositionLongShort == PositionLongShort.Short; } }
		
		public PositionLongShort	PositionLongShort		{ get; protected set; }
		public Bars					Bars					{ get; protected set; }
		public string				Symbol					{ get { return this.Bars.Symbol; } }
		public double				Shares					{ get; protected set; }
		public double				QuoteLast_forMarketOrStopLimit_implicitPrice;// { get; protected set; }

		public PositionPrototype	Prototype;

		public Alert				EntryAlert;				// { get; protected set; }
		public MarketLimitStop		EntryMarketLimitStop	{ get; protected set; }
		public int					EntryFilledBarIndex		{ get; protected set; }
		public Bar					EntryBar				{ get { return this.Bars[this.EntryFilledBarIndex]; } }
		public double				EntryFilled_price		{ get; protected set; }
		public double				EntryEmitted_price;		// { get; protected set; }
		private double				EntryFilled_qty;		// { get; protected set; }
		public double				EntryFilled_slippage	{ get; protected set; }
		public string				EntrySignal				{ get; protected set; }
		public double				EntryFilled_commission	{ get; protected set; }

		public Alert				ExitAlert;				// { get; protected set; }
		public MarketLimitStop		ExitMarketLimitStop		{ get; protected set; }
		public int					ExitFilledBarIndex		{ get; protected set; }
		public Bar					ExitBar					{ get { return this.Bars[this.ExitFilledBarIndex]; } }
		public double				ExitFilled_price		{ get; protected set; }
		public double				ExitEmitted_price;		// { get; protected set; }
		private double				ExitFilled_qty;			// { get; protected set; }
		public double				ExitFilled_slippage		{ get; protected set; }
		public string				ExitSignal				{ get; protected set; }
		public double				ExitFilled_commission	{ get; protected set; }

		public string				StrategyID;
		//public bool NoExitBarOrStreaming { get { return (this.ExitBarIndex == -1 || this.ExitBarIndex == this.Bars.Count); } }
		public bool ExitNotFilledOrStreaming { get {
				if (this.ExitFilledBarIndex == -1) return true;
				if (this.ExitBar == null) return true;
				if (this.ExitBar.IsBarStreaming) return true;
				return false;
			} }
		//public bool EntrySafeToPaint { get { return (this.EntryBar > -1 && this.EntryBar < this.Bars.Count); } }
		//public bool ExitSafeToPaint { get { return (this.ExitBar > -1 && this.ExitBar < this.Bars.Count); } }
		public DateTime EntryDateBarTimeOpen { get {
				//if (this.EntryBarIndex < 0 || this.EntryBarIndex > this.Bars.Count) return DateTime.MinValue;
				Bar barEntry = this.EntryBar;		// don't take it from Alert! dateFilled depends on the market, not on your strategy
				if (barEntry == null) return DateTime.MinValue;
				return barEntry.DateTimeOpen;
			} }
		public DateTime ExitDateBarTimeOpen { get {
				Bar barExit = this.ExitBar;		// don't take it from this.ExitAlert! dateFilled depends on the market, not on your strategy
				if (barExit == null) return DateTime.MinValue;
				return barExit.DateTimeOpen;
			} }
		public double Size { get {
				if (this.Bars.SymbolInfo.SecurityType == SecurityType.Futures) {
					return this.Bars.SymbolInfo.Point2Dollar * this.Shares;
				}
				return this.EntryFilled_price * this.Shares;
			} }

		public double ExitFilled_orBarsClose_forOpenPositions { get {
				double ret = -1;
				if (this.ExitFilled_price != 0 && this.ExitFilled_price != -1) {	//-1 is a standard for justInitialized nonFilled position's Entry/Exit Prices and Bars;
					ret = this.ExitFilled_price;
				} else {
					if (this.Bars.BarStreaming_nullUnsafe == null) {
						throw new Exception("Position.ExitOrStreamingPrice: this.Bars.StreamingBar=null; @ExitBar[" + this.ExitFilledBarIndex + "] position=[" + this + "]; ");
					}
					ret = this.Bars.BarStreaming_nullUnsafe.Close;
				}
				if (this.ExitFilled_slippage != -1) ret += this.ExitFilled_slippage;
				return ret;
			} }
		public double EntryPriceNoSlippage { get {
				double ret = 0;
				if (this.EntryFilled_price == -1) return ret;
				ret = this.EntryFilled_price - this.EntryFilled_slippage;
				return ret;
			} }
		public double ExitOrCurrentPriceNoSlippage { get {
				double ret = this.ExitFilled_orBarsClose_forOpenPositions;
				if (this.ExitFilled_slippage != -1) ret -= this.ExitFilled_slippage;
				return ret;
			} }
		public bool IsEntryFilled { get {
				if (this.EntryFilled_price == -1) return false;
				if (this.EntryFilled_qty == -1) return false;
				if (this.EntryFilled_commission == -1) return false;
				if (this.EntryFilled_slippage == -1) return false;
				return true;
			} }
		public bool IsExitFilled { get {
				if (this.ExitFilled_price == -1) return false;
				if (this.ExitFilled_qty == -1) return false;
				if (this.ExitFilled_commission == -1) return false;
				if (this.ExitFilled_slippage == -1) return false;
				return true;
			} }
		public bool ClosedByTakeProfitLogically { get {
				if (this.EntryFilled_price == -1) {
					throw new Exception("position.EntryPrice=-1, make sure you called EntryFilledWith()");
				}
				if (this.ExitFilled_price == -1) {
					throw new Exception("position.ExitPrice=-1, make sure you called ExitFilledWith()");
				}
				if (this.IsExitFilled == false) {
					throw new Exception("position isn't closed yet, ExitFilled=false");
				}
				bool exitAboveEntry = this.ExitFilled_price > this.EntryFilled_price;

				if (this.PositionLongShort == PositionLongShort.Long) return exitAboveEntry;
				else return !exitAboveEntry;
			} }
		// prototype-related methods
		public bool IsExitFilled_byAlert_prototyped { get {
				this.checkThrow_prototypeNotNull_exitFilled();
				bool oneSideFilled =
					this.ExitAlert == this.Prototype.StopLossAlert_forMoveAndAnnihilation ||
					this.ExitAlert == this.Prototype.TakeProfitAlert_forMoveAndAnnihilation;
				return oneSideFilled;
			} }
		protected void checkThrow_prototypeNotNull_exitFilled() {
			if (this.Prototype == null) {
				#if DEBUG
				Debugger.Break();
				#endif
				throw new Exception("this.Prototype=null, check IsPrototypeNull first");
			}
			if (this.IsExitFilled == false) {
				#if DEBUG
				Debugger.Break();
				#endif
				throw new Exception("position isn't closed yet, ExitFilled=false");
			}
		}
		public bool IsExitFilled_byStopLoss_prototyped { get {
				this.checkThrow_prototypeNotNull_exitFilled();
				if (this.ExitAlert == this.Prototype.StopLossAlert_forMoveAndAnnihilation) return true;
				return false;
			} }
		public bool IsExitFilled_byTakeProfit_prototyped { get {
				this.checkThrow_prototypeNotNull_exitFilled();
				if (this.ExitAlert == this.Prototype.TakeProfitAlert_forMoveAndAnnihilation) return true;
				return false;
			} }
		public Alert PrototypedExitCounterpartyAlert { get {
				if (this.IsExitFilled_byTakeProfit_prototyped) return this.Prototype.StopLossAlert_forMoveAndAnnihilation;
				if (this.IsExitFilled_byStopLoss_prototyped) return this.Prototype.TakeProfitAlert_forMoveAndAnnihilation;
				string msg = "Prototyped position closed by some prototype-unrelated alert[" + this.ExitAlert + "]";
				#if DEBUG
				Debugger.Break();
				#endif
				throw new Exception(msg);
			} }
		
		public void Dispose() {
			if (this.IsDisposed) {
				string msg = "ALREADY_DISPOSED__DONT_INVOKE_ME_TWICE__" + this.ToString();
				//Assembler.PopupException(msg);
				return;
			}
			if (this.EntryAlert != null) {
				this.EntryAlert.Dispose();
				//LATE_CALLBACKS_TOO_NOISY this.EntryAlert = null;
			}
			if (this.ExitAlert != null) {
				this.ExitAlert.Dispose();
				//LATE_CALLBACKS_TOO_NOISY this.ExitAlert = null;
			}
			this.IsDisposed = true;
		}
		public bool IsDisposed { get; private set; }

		~Position() { this.Dispose(); }
		public Position() {
			string msig = "THIS_CTOR_IS_INVOKED_BY_JSON_DESERIALIZER__KEEP_ME_PUBLIC__CREATE_[JsonIgnore]d_VARIABLES_HERE";
			
			PositionLongShort = PositionLongShort.Unknown;
			StrategyID = "STRATEGY_ID_NOT_INITIALIZED";

			EntryMarketLimitStop = MarketLimitStop.Unknown;
			EntryFilledBarIndex = -1;
			EntryFilled_price = -1;
			EntryFilled_qty = -1;
			EntryFilled_slippage = -1;
			EntryFilled_commission = -1;

			ExitMarketLimitStop = MarketLimitStop.Unknown;
			ExitFilledBarIndex = -1;
			ExitFilled_price = -1;
			ExitFilled_qty = -1;
			ExitFilled_slippage = -1;
			ExitFilled_commission = -1;
		}
		Position(Bars bars, PositionLongShort positionLongShort, string strategyID, double basisPrice, double shares) : this() {
			this.Bars = bars;
			this.PositionLongShort = positionLongShort;
			this.StrategyID = strategyID;
			this.QuoteLast_forMarketOrStopLimit_implicitPrice = basisPrice;
			this.Shares = shares;
		}
		public Position(Alert alertEntry, double basisPrice) : this(alertEntry.Bars
				, alertEntry.PositionLongShortFromDirection, alertEntry.StrategyID.ToString()
				, basisPrice, alertEntry.Qty) {
			this.EntryAlert = alertEntry;
			this.EntryMarketLimitStop = alertEntry.MarketLimitStop;
			this.EntryEmitted_price = alertEntry.PriceEmitted;
			this.EntrySignal = alertEntry.SignalName;
		}
		public void ExitAlertAttach(Alert alertExit) {
			if (this.Prototype == null) {
				if (this.ExitAlert != null) {
					string msg = "POSITION_WAS_ALREADY_ATTCHED_TO_EXIT_ALERT ExitAlert[" + this.ExitAlert + "] ";
					#if DEBUG
					Debugger.Break();
					#endif
					throw new Exception(msg);
				}
				if (this.ExitMarketLimitStop != MarketLimitStop.Unknown) {
					string msg = "POSITION_WAS_ALREADY_SYNCHED_WITH_FILLED_ALERT_ON_ALERT_FILLED_CALLBACK: ExitPriceScript["
						+ this.ExitEmitted_price + "]";
					#if DEBUG
					Debugger.Break();
					#endif
					throw new Exception(msg);
				}
			} else {
				if (this.ExitAlert != null) {
					if (alertExit.IsFilled == false) {
						string msg = "REPLACING_FIRST_CREATED_STOPLOSS_WITH_SECOND_CREATED_TAKEPROFIT ExitAlert[" 
							+ this.ExitAlert.SignalName + "] shouldBeTakeprofit[" + alertExit.SignalName + "]";
					} else {
						string msg = "REPLACING_EXIT_ALERT_WITH_PROTOTYPED_TP_OR_SL_WHICHEVER_FILLED_FIRST ExitAlert["
							+ this.ExitAlert + "] filledFirst[" + alertExit + "]";
					}
				}
			}
			this.ExitAlert = alertExit;
			this.ExitMarketLimitStop = alertExit.MarketLimitStop;
			this.ExitEmitted_price = alertExit.PriceEmitted;
			this.ExitSignal = alertExit.SignalName;
		}
		public void FillEntryWith(Bar entryBar, double entryFill_price, double entryFill_qty, double entryFill_slippage, double entryFill_commission) {
			string msig = " FillEntryWith(" + entryBar + ", " + entryFill_price + ", " + entryFill_qty + ", " + entryFill_slippage + ", " + entryFill_commission + ")";
			string alertOpenedThisPosition = (this.EntryAlert == null) ? "NO_ENTRY_ALERT" : this.EntryAlert.ToString();
			// 1) absolutely acceptable to have a limit order beoynd the bar;
			// 2) Market order must be filled now at SpreadGenerator-generated ANY price while StreamingBar may contain only 1 quote (height=0)
			//if (entryBar.ContainsPrice(entryFillPrice) == false) {
			//	string msg = "PRICE_FILLED_POSITION_ENTRY_DOESNT_EXIST_IN_ENTRYBAR entryFilledPrice[" + entryFillPrice + "] entryBar[" + entryBar + "]";
			//	throw new Exception(msg + msig);
			//}
			if (entryBar.Volume < entryFill_qty) {
				string msg = "VOLUME_FILLED_POSITION_ENTRY_NEVER_TRADED_DURING_THE_ENTRYBAR entryFilledQty["
					+ entryFill_qty + "] entryBar.Volume[" + entryBar.Volume + "]";
				#if DEBUG
				Debugger.Break();
				#endif
				throw new Exception(msg + msig);
			}
			if (this.EntryFilledBarIndex != -1) {
				string msg = "PositionEntry was already filled earlier @EntryBar[" + this.EntryFilledBarIndex + "]"
						+ ", you can't override it with [" + entryBar + "]; alertOpenedThisPosition["
						+ alertOpenedThisPosition + "]";
				#if DEBUG
				Debugger.Break();
				#endif
				throw new Exception(msg + msig);
			}
			this.EntryFilledBarIndex = entryBar.ParentBarsIndex;
			if (this.EntryBar == null) {
				string msg = "BARINDEX_FILLED_POSITION_ENTRY_DOESNT_BELONG_TO_ITS_OWN_PARENT_CHECK_Position.EntryBar_PROPERTY entryBar["
					+ entryBar + "] this.Bars[" + this.Bars + "]";
				#if DEBUG
				Debugger.Break();
				#endif
				throw new Exception(msg + msig);
			}
			if (entryBar.Open != this.EntryBar.Open) {
				string msg = "BAR_FILLED_POSITION_ENTRY_DOESNT_HAVE_SAME_OPEN_CHECK_Position.EntryBar_PROPERTY entryBar.Open["
					+ entryBar.Open + "] this.Bars[" + this.EntryFilledBarIndex + "].Open[" + entryBar.Open + "]";
				#if DEBUG
				Debugger.Break();
				#endif
				throw new Exception(msg + msig);
			}
			this.EntryFilled_price		= entryFill_price;
			this.EntryFilled_qty		= entryFill_qty;
			this.EntryFilled_slippage	= entryFill_slippage;
			this.EntryFilled_commission	= entryFill_commission;
		}
		public void FillExitWith(Bar exitBar, double exitFill_price, double exitFill_qty, double exitFill_slippage, double exitFill_commission) {
			string msig = " FillExitWith(" + exitBar + ", " + exitFill_price + ", " + exitFill_qty + ", " + exitFill_slippage + ", " + exitFill_commission + ")";
			// 1) absolutely acceptable to have a limit order beoynd the bar;
			// 2) Market order must be filled now at SpreadGenerator-generated ANY price while StreamingBar may contain only 1 quote (height=0)
			//if (exitBar.ContainsPrice(exitFillPrice) == false) {
			//	string msg = "PRICE_FILLED_POSITION_EXIT_DOESNT_EXIST_IN_EXITBAR exitFilledPrice[" + exitFillPrice + "] exitBar[" + exitBar + "]";
			//	throw new Exception(msg + msig);
			//}
			if (exitBar.Volume < exitFill_qty) {
				string msg = "VOLUME_FILLED_POSITION_EXIT_NEVER_TRADED_DURING_THE_EXITBAR exitFilledQty["
					+ exitFill_qty + "] exitBar.Volume[" + exitBar.Volume + "]";
				#if DEBUG
				Debugger.Break();
				#endif
				throw new Exception(msg + msig);
			}
			if (this.EntryFilledBarIndex == -1) {
				string msg = "ATTEMPT_TO_CLOSE_NON_OPENED_POSITION this.EntryBarIndex=-1 " + this;
				#if DEBUG
				Debugger.Break();
				#endif
				throw new Exception(msg + msig);
			}
			if (this.EntryFilledBarIndex > exitBar.ParentBarsIndex) {
				string msg = "ATTEMPT_TO_CLOSE_POSITION_AT_BAR_EARLIER_THAN_POSITION_WAS_OPENED this.EntryBarIndex["
					+ this.EntryFilledBarIndex + "] > exitBar.ParentBarsIndex[" + exitBar.ParentBarsIndex + "]";
				#if DEBUG
				//I_FORGIVE_YOU_WERE_IGNORANT_closePositionsLeftOpenAfterBacktest  Debugger.Break();
				#endif
				throw new Exception(msg + msig);
			}
			if (this.ExitFilledBarIndex != -1) {
				string alertClosedThisPosition = (this.ExitAlert == null) ? "NO_EXIT_ALERT" : this.ExitAlert.ToString();
				string msg = "PositionExit was already filled earlier @ExitBar[" + this.ExitFilledBarIndex + "]"
						+ ", you can't override it with [" + exitBar + "]; alertClosedThisPosition[" + alertClosedThisPosition + "]";
				#if DEBUG
				Debugger.Break();
				#endif
				throw new Exception(msg);
			}
			this.ExitFilledBarIndex = exitBar.ParentBarsIndex;

			if (this.ExitBar == null) {
				string msg = "BARINDEX_FILLED_POSITION_EXIT_DOESNT_BELONG_TO_ITS_OWN_PARENT_BARS_CHECK_Position.EntryBar_PROPERTY exitBar["
					+ exitBar + "] this.Bars[" + this.Bars + "]";
				#if DEBUG
				Debugger.Break();
				#endif
				throw new Exception(msg + msig);
			}
			if (exitBar.Open != this.ExitBar.Open) {
				string msg = "BAR_FILLED_POSITION_EXIT_DOESNT_HAVE_SAME_OPEN_CHECK_Position.EntryBar_PROPERTY exitBar.Open["
					+ exitBar.Open + "] this.Bars[" + this.ExitFilledBarIndex + "].Open[" + exitBar.Open + "]";
				#if DEBUG
				Debugger.Break();
				#endif
				throw new Exception(msg + msig);
			}

			if (exitBar.ParentBars != this.EntryBar.ParentBars) {
				bool skipWhenRemovingPendingAlertBacktestLeftOpen = this.EntryBar.ParentBars.ReasonToExist.Contains(Backtester.BARS_BACKTEST_CLONE_PREFIX)
					&& exitBar.ParentBars.ReasonToExist.Contains(Backtester.BARS_BACKTEST_CLONE_PREFIX) == false;

				if (skipWhenRemovingPendingAlertBacktestLeftOpen == false) {
					skipWhenRemovingPendingAlertBacktestLeftOpen = this.EntryBar.ParentBars.ReasonToExist.Contains(Livesimulator.BARS_LIVESIM_CLONE_PREFIX)
						&& exitBar.ParentBars.ReasonToExist.Contains(Livesimulator.BARS_LIVESIM_CLONE_PREFIX) == false;
				}

				if (skipWhenRemovingPendingAlertBacktestLeftOpen) {
					string msg = "NOW_LOOK_AT_CALLSTACK__3_LEVELS_LOWER_YOU_SHOULD_SEE__ScriptExecutor.RemovePendingExitAlertPastDueClosePosition()";
				} else {
					string msg = "PARENTS_OF_BAR_FILLED_POSITION_EXIT_MUST_BE_SAME_AS_ENTRY_BAR_PARENTS exitBar.ParentBars["
						+ exitBar.ParentBars + "] != this.EntryBar.ParentBars[" + this.EntryBar.ParentBars + "]";
					#if DEBUG
					Debugger.Break();
					#endif
					throw new Exception(msg + msig);
				}
			}
			this.ExitFilled_price		= exitFill_price;
			this.ExitFilled_qty			= exitFill_qty;
			this.ExitFilled_slippage	= exitFill_slippage;
			this.ExitFilled_commission	= exitFill_commission;
		}
		public override string ToString() {
			StringBuilder msg = new StringBuilder();
			if (this.SernoAbs > 0) {
				msg.Append("#");
				msg.Append(this.SernoAbs);
			}
			msg.Append(this.PositionLongShort);
			msg.Append(" ");
			msg.Append(Shares);
			msg.Append("*");
			msg.Append(this.Symbol);
			msg.Append(" Entry=[");
			if (this.EntryFilledBarIndex != -1) {
				msg.Append(EntryMarketLimitStop);
				msg.Append("@");
				msg.Append(EntryFilled_price);
				msg.Append("/bar");
				msg.Append(EntryFilledBarIndex);
				msg.Append(":");
				if (this.EntryAlert != null) {
					if (this.EntryAlert.OrderFollowed != null) {
						msg.Append(this.EntryAlert.OrderFollowed.State);
					} else {
						msg.Append("NO_ENTRY_ORDER");
					}
				} else {
					msg.Append("NO_ENTRY_ALERT");
				}
			} else {
				msg.Append("ENTRY_BAR-1");
			}
			msg.Append("] Exit=[");
			if (this.ExitFilledBarIndex != -1) {
				msg.Append(ExitMarketLimitStop);
				msg.Append("@");
				msg.Append(ExitFilled_price);
				msg.Append("/bar");
				msg.Append(ExitFilledBarIndex);
				msg.Append(":");
				if (this.ExitAlert != null) {
					if (this.ExitAlert.OrderFollowed != null) {
						msg.Append(this.ExitAlert.OrderFollowed.State);
					} else {
						msg.Append("NO_EXIT_ORDER");
					}
				} else {
					msg.Append("NO_EXIT_ALERT");
				}
			} else {
				msg.Append("EXIT_BAR-1");
			}
			msg.Append("]");
			if (this.QuoteLast_forMarketOrStopLimit_implicitPrice != 0) {
				msg.Append(" BasisPrice[");
				msg.Append(this.QuoteLast_forMarketOrStopLimit_implicitPrice);
				msg.Append("]");
			}
			if (this.Prototype != null) {
				msg.Append(" Proto");
				msg.Append(this.Prototype.ToString());
			}
			return msg.ToString();

		}
	}
}