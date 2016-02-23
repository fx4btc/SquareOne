﻿using System;
using System.Drawing;
using System.Windows.Forms;

//http://www.codeproject.com/Articles/12870/Don-t-Flicker-Double-Buffer
namespace Sq1.Core.DoubleBuffered {
	public abstract class UserControlDoubleBuffered : UserControl {
		protected BufferedGraphicsContext graphicManager;
		public BufferedGraphics BufferedGraphics;
		
		//protected abstract void OnPaintDoubleBuffered(PaintEventArgs pe);
		protected virtual void OnPaintDoubleBuffered(PaintEventArgs pe) {
			base.OnPaint(pe);
		}
		protected virtual void OnPaintBackgroundDoubleBuffered(PaintEventArgs pe) {
			pe.Graphics.Clear(base.BackColor);
			//NOPE base.OnPaintBackground(pe);	// horizontal Panels inside MultiSplitter will receive PaintBackground on MultiSplitter.Invalidate()
		}
		
		// what sort of TRANSPARENT does it allow?... ommitting will require "override OnResize()"
		//ABSOLUTELY_FLICKERS protected override CreateParams CreateParams { get {
		//	CreateParams cp = base.CreateParams;
		//	cp.ExStyle |= 0x00000020; //WS_EX_TRANSPARENT
		//	return cp;
		//} }

		public UserControlDoubleBuffered() : base() {
			Application.ApplicationExit += new EventHandler(disposeAndNullifyToRecreateInPaint);
			//base.SetStyle( ControlStyles.AllPaintingInWmPaint
			//			 | ControlStyles.OptimizedDoubleBuffer
			//		//	 | ControlStyles.UserPaint
			//		//	 | ControlStyles.ResizeRedraw
			//		, true);
			this.graphicManager = BufferedGraphicsManager.Current;
		}
		void initializeBuffer() {
			this.graphicManager.MaximumBuffer =  new Size(base.Width + 1, base.Height + 1);
			//v1 this.BufferedGraphics = this.graphicManager.Allocate(this.CreateGraphics(),  base.ClientRectangle);
			// http://dotnetfacts.blogspot.ca/2008/03/things-you-must-dispose.html
			Graphics gNew = this.CreateGraphics();
			this.BufferedGraphics = this.graphicManager.Allocate(gNew, base.ClientRectangle);
			gNew.Dispose();
		}
		void disposeAndNullifyToRecreateInPaint(object sender, EventArgs e) {
			if (this.BufferedGraphics == null) return;
			this.BufferedGraphics.Dispose();
			this.BufferedGraphics = null;
		}
		protected override void OnPaint(PaintEventArgs pe) {
			try {
				// overhead here since we need to call this.InitializeBuffer() in ctor() after
				// UserControlChild.InitializeComponents() where UserControl.Width and UserControl.Height are set
				if (this.BufferedGraphics == null) this.initializeBuffer();
				
				// let the child draw on BufferedGraphics
				PaintEventArgs peSubstituted = new PaintEventArgs(BufferedGraphics.Graphics, pe.ClipRectangle);
				// REVERTED_TO_KEEP_WINFORMS_COMPATIBLE: child has to clip after resize
				//PaintEventArgs peSubstituted = new PaintEventArgs(BufferedGraphics.Graphics, base.ClientRectangle);
				//pe.Graphics.SetClip(base.ClientRectangle);	// always repaint whole UserControl Surface; by default, only extended area is "Clipped"
				
				//NO_MANUAL_BG_FOLLOW_WINFORMS_MODEL this.OnPaintBackgroundDoubleBuffered(peSubstituted);
				this.OnPaintDoubleBuffered(peSubstituted);
				//OVERHEAD_REMOVED base.OnPaint(peSubstituted);
	
				// now we spit BufferedGraphics into the screen
				this.BufferedGraphics.Render(pe.Graphics);
			} catch (Exception ex) {
				string msg = "USER_CONTROL_DOUBLE_BUFFERED.OnPaint()_HAS_PROBLEMS_WITH_DOUBLE_BUFFERING_API"
					+ " OTHERWIZE_REFACTOR_CHILDREN_TO_CATCH_THEIR_OWN_EXCEPTIONS";
				Assembler.PopupException(msg, ex);
			}
		}
		protected override void OnPaintBackground(PaintEventArgs pe) {
			try {
				// overhead here since we need to call this.InitializeBuffer() in ctor() after
				// UserControlChild.InitializeComponents() where UserControl.Width and UserControl.Height are set
				if (this.BufferedGraphics == null) this.initializeBuffer();
				PaintEventArgs peSubstituted = new PaintEventArgs(BufferedGraphics.Graphics, pe.ClipRectangle);
				this.OnPaintBackgroundDoubleBuffered(peSubstituted);

				// WHY_THIS_WASNT_HERE_BEFORE?? TRYING_TO_FIX_SPLITTERS_BECOMING_GRAY_ONLY_AFTER_MOUSEOVER
				// DOESNT_HELP_THE_SPLITTERS this.BufferedGraphics.Render(pe.Graphics);
				//NOPE base.OnPaintBackground(pe);	// horizontal Panels inside MultiSplitter will receive PaintBackground on MultiSplitter.Invalidate()
			} catch (Exception ex) {
				string msg = "USER_CONTROL_DOUBLE_BUFFERED.OnPaint()_HAS_PROBLEMS_WITH_DOUBLE_BUFFERING_API"
					+ " OTHERWIZE_REFACTOR_CHILDREN_TO_CATCH_THEIR_OWN_EXCEPTIONS";
				Assembler.PopupException(msg, ex);
			}
		}
		protected override void OnResize(EventArgs e) {
			try {
				//v1 NOPE_POSTPONING_CREATION_UNTIL_PAINT_FOREGROUND_WILL_MAKE_BACKGROUND_PAINT_NPE
				this.disposeAndNullifyToRecreateInPaint(this, e);
				//v2 CREATING_EXPLICITLY_MAY_BE_REDUNDANT this.BufferReset();
				base.Invalidate();		// for RangeBar consisting of no inner controls it was enough, but  
				//base.PerformLayout();		// ChartShadow : UserControlDoubleBuffered wasn't resizing it's inner Splitters with Dock=Fill
				base.OnResize(e);			// I_NEED_TO_PLACE hscroll at the bottom!
			} catch (Exception ex) {
				string msg = "USER_CONTROL_DOUBLE_BUFFERED.OnPaint()_HAS_PROBLEMS_WITH_DOUBLE_BUFFERING_API"
					+ " OTHERWIZE_REFACTOR_CHILDREN_TO_CATCH_THEIR_OWN_EXCEPTIONS";
				Assembler.PopupException(msg, ex);
			}
		}
		public void BufferReset() {
			this.disposeAndNullifyToRecreateInPaint(this, null);
			this.initializeBuffer();
			//THROWS_IF_OnResize()_UPSTACK base.Invalidate();
		}
	}
}