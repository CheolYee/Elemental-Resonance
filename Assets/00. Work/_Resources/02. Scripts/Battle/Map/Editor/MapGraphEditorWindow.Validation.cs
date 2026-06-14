using UnityEngine;
using UnityEngine.UIElements;

namespace Battle.Map.Editor
{
    public sealed partial class MapGraphEditorWindow
    {
        private Label _validationStatusLabel;

        private void BuildValidationContent()
        {
            _validationPanel.Add(MakeSectionHeader("Validation"));

            _validationStatusLabel = new Label("— (Phase 7-3에서 구현)")
            {
                style =
                {
                    fontSize = 10,
                    color    = new StyleColor(new Color(0.50f, 0.50f, 0.55f))
                }
            };
            _validationPanel.Add(_validationStatusLabel);
        }

        private void RefreshValidation()
        {
            // Phase 7-3에서 구현
        }
    }
}
