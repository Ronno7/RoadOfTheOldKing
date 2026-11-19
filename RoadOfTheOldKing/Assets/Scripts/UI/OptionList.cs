using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace RoadOfTheOldKing.UI
{
    // Settings-style rows (user, 15 Nov: "add sliders where appropriate for sensible layout"): each row is a label with
    // its control beside it, grouped under small headings, as most games' option screens do. Up/down chooses a row,
    // left/right changes it, Enter/A presses a button row; the mouse can pick a segment, an arrow or a swatch. Shared
    // by the settings page (pause and title) and the hoodie picker. Styles: option-* in MenuTheme.uss.
    public sealed class OptionList
    {
        public abstract class Row
        {
            public readonly string Label;
            public readonly string Description;
            public virtual bool Selectable => true;
            internal VisualElement Element;
            internal OptionList Owner;
            protected Row(string label, string description) { Label = label; Description = description ?? ""; }
            internal virtual VisualElement Build()
            {
                var row = new VisualElement { pickingMode = PickingMode.Position };
                row.AddToClassList("kit-row"); row.AddToClassList("option-row");
                var cursor = new VisualElement { pickingMode = PickingMode.Ignore };
                cursor.AddToClassList("kit-cursor");
                row.Add(cursor);
                var name = new Label(Label) { pickingMode = PickingMode.Ignore };
                name.AddToClassList("option-label");
                row.Add(name);
                var control = new VisualElement();
                control.AddToClassList("option-control");
                row.Add(control);
                BuildControl(control);
                return row;
            }
            protected virtual void BuildControl(VisualElement control) { }
            public virtual void Adjust(int direction) { }
            public virtual void Submit() => Owner?.Navigate(1);
            public virtual void Refresh() { }
            protected void Changed() => Owner?.RowChanged(this);

            protected Label Arrow(string glyph, int direction)
            {
                var arrow = new Label(glyph);
                arrow.AddToClassList("option-arrow");
                arrow.RegisterCallback<PointerDownEvent>(e => { Owner?.Select(this); Adjust(direction); e.StopPropagation(); });
                return arrow;
            }
        }

        // A small gold heading over a group of rows; never selected.
        public sealed class Header : Row
        {
            public Header(string label) : base(label, "") { }
            public override bool Selectable => false;
            internal override VisualElement Build()
            {
                var heading = new Label(Label) { pickingMode = PickingMode.Ignore };
                heading.AddToClassList("option-header");
                return heading;
            }
        }

        // A bar of segments. Filled (volume, strengths), or a colour track with a marker on the chosen step (the hoodie).
        public sealed class Slider : Row
        {
            private readonly Func<float> get;
            private readonly Action<float> set;
            private readonly int steps;
            private readonly Func<float, string> valueText;
            private readonly Func<int, Color> trackColor;
            private readonly List<VisualElement> segments = new List<VisualElement>();
            private Label value;

            // steps: the number of moves from 0 to 1 (10 = 0%, 10% ... 100%). trackColor: a colour per segment for a
            // colour track; null for a filled bar.
            public Slider(string label, string description, Func<float> get, Action<float> set, int steps,
                Func<float, string> valueText = null, Func<int, Color> trackColor = null) : base(label, description)
            { this.get = get; this.set = set; this.steps = Mathf.Max(1, steps); this.valueText = valueText; this.trackColor = trackColor; }

            private int Step => Mathf.Clamp(Mathf.RoundToInt(get() * steps), 0, steps);

            protected override void BuildControl(VisualElement control)
            {
                control.Add(Arrow("<", -1));
                var track = new VisualElement();
                track.AddToClassList("option-track");
                int count = trackColor != null ? steps + 1 : steps;
                for (int i = 0; i < count; i++)
                {
                    int index = i;
                    var segment = new VisualElement();
                    segment.AddToClassList("option-segment");
                    if (trackColor != null) segment.AddToClassList("option-swatch-segment");
                    segment.RegisterCallback<PointerDownEvent>(e =>
                    {
                        Owner?.Select(this);
                        set(trackColor != null ? index / (float)steps : (index + 1) / (float)steps);
                        Changed();
                        e.StopPropagation();
                    });
                    segments.Add(segment);
                    track.Add(segment);
                }
                control.Add(track);
                control.Add(Arrow(">", 1));
                value = new Label();
                value.AddToClassList("option-value");
                if (valueText != null) control.Add(value);
                Refresh();
            }

            public override void Adjust(int direction)
            {
                int next = Mathf.Clamp(Step + direction, 0, steps);
                if (next == Step) return;
                set(next / (float)steps);
                Changed();
            }

            public override void Refresh()
            {
                int step = Step;
                for (int i = 0; i < segments.Count; i++)
                {
                    if (trackColor != null)
                    {
                        segments[i].style.backgroundColor = trackColor(i);
                        segments[i].EnableInClassList("picked", i == step);
                    }
                    else segments[i].EnableInClassList("on", i < step);
                }
                if (value != null && valueText != null) value.text = valueText(get());
            }
        }

        // One of a few named values, stepped with the arrows: "< Fullscreen >".
        public sealed class Choice : Row
        {
            private readonly string[] options;
            private readonly Func<int> get;
            private readonly Action<int> set;
            private Label value;

            public Choice(string label, string description, string[] options, Func<int> get, Action<int> set) : base(label, description)
            { this.options = options; this.get = get; this.set = set; }

            protected override void BuildControl(VisualElement control)
            {
                control.Add(Arrow("<", -1));
                value = new Label();
                value.AddToClassList("option-choice");
                value.RegisterCallback<PointerDownEvent>(e => { Owner?.Select(this); Adjust(1); e.StopPropagation(); });
                control.Add(value);
                control.Add(Arrow(">", 1));
                Refresh();
            }

            public override void Adjust(int direction)
            {
                set(((get() + direction) % options.Length + options.Length) % options.Length);
                Changed();
            }

            public override void Submit() => Adjust(1);
            public override void Refresh() { if (value != null) value.text = options[Mathf.Clamp(get(), 0, options.Length - 1)]; }
        }

        // A row of colour squares; the chosen one is framed in gold.
        public sealed class Swatches : Row
        {
            private readonly Color[] colors;
            private readonly Func<int> get;
            private readonly Action<int> set;
            private readonly List<VisualElement> squares = new List<VisualElement>();

            // get returns -1 when no swatch matches (a colour made on the sliders).
            public Swatches(string label, string description, Color[] colors, Func<int> get, Action<int> set) : base(label, description)
            { this.colors = colors; this.get = get; this.set = set; }

            protected override void BuildControl(VisualElement control)
            {
                for (int i = 0; i < colors.Length; i++)
                {
                    int index = i;
                    var square = new VisualElement();
                    square.AddToClassList("option-swatch");
                    square.style.backgroundColor = colors[i];
                    square.RegisterCallback<PointerDownEvent>(e => { Owner?.Select(this); set(index); Changed(); e.StopPropagation(); });
                    squares.Add(square);
                    control.Add(square);
                }
                Refresh();
            }

            public override void Adjust(int direction)
            {
                int current = get();
                int next = current < 0 ? (direction > 0 ? 0 : colors.Length - 1) : ((current + direction) % colors.Length + colors.Length) % colors.Length;
                set(next);
                Changed();
            }

            public override void Refresh()
            {
                int current = get();
                for (int i = 0; i < squares.Count; i++) squares[i].EnableInClassList("picked", i == current);
            }
        }

        // A button row (Back, Begin): centred text, pressed with Enter/A or a click.
        public sealed class Command : Row
        {
            private readonly Action run;
            public Command(string label, string description, Action run) : base(label, description) { this.run = run; }
            internal override VisualElement Build()
            {
                var row = new VisualElement();
                row.AddToClassList("kit-row"); row.AddToClassList("option-command");
                var cursor = new VisualElement { pickingMode = PickingMode.Ignore };
                cursor.AddToClassList("kit-cursor");
                row.Add(cursor);
                var text = new Label(Label) { pickingMode = PickingMode.Ignore };
                row.Add(text);
                row.RegisterCallback<ClickEvent>(_ => { Owner?.Select(this); run?.Invoke(); });
                return row;
            }
            public override void Submit() => run?.Invoke();
        }

        private readonly VisualElement container;
        private readonly Label description;
        private readonly List<Row> rows = new List<Row>();
        private int selected = -1;

        // Called after any row changes a value, so rows that depend on it (a hoodie track) can redraw.

        public OptionList(VisualElement container, Label description)
        {
            this.container = container;
            this.description = description;
        }

        public IReadOnlyList<Row> Rows => rows;
        public int Selected => selected;
        public Row SelectedRow => selected >= 0 && selected < rows.Count ? rows[selected] : null;

        public void Show(IReadOnlyList<Row> items, int select = 0)
        {
            rows.Clear();
            container.Clear();
            foreach (var row in items)
            {
                row.Owner = this;
                row.Element = row.Build();
                if (row.Selectable)
                {
                    var captured = row;
                    row.Element.RegisterCallback<PointerEnterEvent>(_ => Select(captured));
                }
                rows.Add(row);
                container.Add(row.Element);
            }
            selected = -1;
            int index = -1;
            for (int i = 0, n = 0; i < rows.Count; i++)
                if (rows[i].Selectable) { if (n == select) { index = i; break; } n++; }
            if (index < 0) index = Next(-1, 1);
            Highlight(index);
        }

        // Selectable rows only, counted from the top: the same numbering Show's select uses.
        public int SelectedIndex
        {
            get { int n = 0; for (int i = 0; i < rows.Count; i++) if (rows[i].Selectable) { if (i == selected) return n; n++; } return -1; }
        }

        public void Navigate(int direction)
        {
            if (rows.Count == 0) return;
            int next = Next(selected, direction > 0 ? 1 : -1);
            if (next >= 0) Highlight(next);
        }

        public void Adjust(int direction) => SelectedRow?.Adjust(direction);
        public void Submit() => SelectedRow?.Submit();
        public void RefreshAll() { foreach (var row in rows) row.Refresh(); }

        internal void Select(Row row)
        {
            int index = rows.IndexOf(row);
            if (index >= 0 && row.Selectable) Highlight(index);
        }

        internal void RowChanged(Row row)
        {
            RefreshAll();
        }

        private int Next(int from, int step)
        {
            for (int i = 1; i <= rows.Count; i++)
            {
                int index = ((from + step * i) % rows.Count + rows.Count) % rows.Count;
                if (rows[index].Selectable) return index;
            }
            return -1;
        }

        private void Highlight(int index)
        {
            selected = index;
            for (int i = 0; i < rows.Count; i++) rows[i].Element.EnableInClassList("selected", i == selected);
            if (description != null) description.text = SelectedRow != null ? SelectedRow.Description : "";
        }
    }
}
