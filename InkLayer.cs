using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace NDI_Telestrator
{
    public class InkLayer : System.Windows.Controls.InkCanvas
    {

        public event EventHandler LayerUpdated;

        public Queue<Stroke> redoQueue;

        // The stylus/touch ink doesn't get captured via the RenderTargetBitmap
        // function so we'll add it to a Stroke that we add it in
        StylusPointCollection stylusStrokeBuffer = null;

        public InkLayer(Canvas parent) : base()
        {
            Background = System.Windows.Media.Brushes.Transparent;
            UseCustomCursor = true;
            Cursor = parent.Cursor;
            Width = parent.Width;
            Height = parent.Height;
            redoQueue = new Queue<Stroke>();
            parent.SizeChanged += (a, b) =>
            {
                Width = b.NewSize.Width;
                Height = b.NewSize.Height;
            };
        }

        // Generate a bitmap of the individual layer
        public BitmapFrame Draw(Brush background = null)
        {
            DrawingVisual drawingVisual = new DrawingVisual();
            using (DrawingContext drawingContext = drawingVisual.RenderOpen())
            {
                if (background != null) drawingContext.DrawRectangle(background, null, new Rect(0, 0, (int)Width, (int)Height));
                Strokes.Draw(drawingContext);
                drawingContext.Close();

                RenderTargetBitmap rtb = new RenderTargetBitmap((int)Width, (int)Height, 96d, 96d, PixelFormats.Default);
                rtb.Render(drawingVisual);

                return BitmapFrame.Create(rtb);
            }
        }

        public void Undo()
        {
            if (Strokes.Count > 0)
            {
                redoQueue.Enqueue(Strokes.Last());
                Strokes.RemoveAt(Strokes.Count - 1);
                LayerUpdated?.Invoke(this, null);
            }
        }

        public void Redo()
        {
            if (redoQueue.Count > 0)
            {
                Strokes.Add(redoQueue.Dequeue());
                LayerUpdated?.Invoke(this, null);
            }
        }

        private void _handleStrokeCollection(Stroke stroke)
        {
            // Clear the redo queue on new stroke input
            // TODO: Check if working wtih stroke move / copy / drag
            redoQueue.Clear();
            LayerUpdated?.Invoke(this, null);
        }

        protected override void OnStrokeCollected(InkCanvasStrokeCollectedEventArgs e)
        {
            // Do nothing
            // Don't let anyone use this uwu
            // base.OnStrokeCollected(e);
        }

        protected override void OnPreviewStylusDown(StylusDownEventArgs e)
        {
            Strokes.Add(new Stroke(stylusStrokeBuffer = e.StylusDevice.GetStylusPoints(this), DefaultDrawingAttributes));

            // Handling here slows the rendering
            // But not handling adds a 1-pixel (or so) stroke
            // EDIT: If PreviewStylusUp is handled, the 1-pixel stroke is not added
            // b.Handled = true;

            base.OnPreviewStylusDown(e);
        }

        protected override void OnPreviewMouseDown(MouseButtonEventArgs e)
        {
            // Cancel mouse down event if the stylus was used (Prevents single pixel stroke)
            if (stylusStrokeBuffer != null)
            {
                e.Handled = true;
                return;
            }

            base.OnPreviewMouseDown(e);
        }

        //protected override void OnPreviewStylusUp(StylusEventArgs e)
        //{
        //    // Clear the buffer when the stylus is lifted
        //    //     // stylusStrokeBuffer = null;

        //    //     // Manually trigger events
        //    //     // RaiseEvent(new InkCanvasStrokeCollectedEventArgs(Strokes[Strokes.Count - 1]) { RoutedEvent = InkCanvas.StrokeCollectedEvent });

        //    //     // Blocking this event stops the 1-pixel stroke from being added
        //    //     // But currently breaks mouse use (need to release the stylus / mouse?)
        //    //     // EDIT: For now we'll just remove it I guess..
        //    //     // e.Handled = true;
        //    base.OnPreviewStylusUp(e);
        //}

        // The 1-pixel stroke gets added somewhere between PreviewStylusUp and StylusUp

        protected override void OnStylusUp(StylusEventArgs e)
        {
            // Remove the last stroke (1)
            stylusStrokeBuffer = null;
            Strokes.RemoveAt(Strokes.Count - 1);

            // Using OnMouseLeftButtonUp instead now
            // OnStrokeCollected(new InkCanvasStrokeCollectedEventArgs(Strokes[Strokes.Count - 1]) { RoutedEvent = InkCanvas.StrokeCollectedEvent });
       
            base.OnStylusUp(e);
        }
        protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
        {
            _handleStrokeCollection(Strokes[Strokes.Count - 1]);
            base.OnMouseLeftButtonUp(e);
        }

        protected override void OnPreviewStylusMove(StylusEventArgs e)
        {
            // Add points to the buffer
            stylusStrokeBuffer.Add(e.StylusDevice.GetStylusPoints(this));

            // Blocks events that would populate the 1-pixel stroke
            e.Handled = true;
            
            // base.OnPreviewStylusMove(e);
        }
    }
}
