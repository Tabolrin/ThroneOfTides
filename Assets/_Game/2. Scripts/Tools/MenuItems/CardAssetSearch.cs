using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;
using ThroneOfTides.Data;

namespace ThroneOfTides.Tools
{
    public static class CardAssetSearch
    {
        // ── Menu Items ──────────────────────────────────────────────────────────

        [MenuItem("ThroneOfTides/Validate All Cards  %#v")]   // Ctrl+Shift+V
        public static void ValidateAllCards()
        {
            var (errors, warnings) = ValidateAllData();

            var sb = new StringBuilder();
            var cards = LoadAll<CardSO>();
            sb.AppendLine($"Validated {cards.Count} cards.\n");

            if (errors.Count > 0)
            {
                sb.AppendLine($"── {errors.Count} ERROR(S) ──");
                foreach (var e in errors) sb.AppendLine("  ✗ " + e);
            }
            if (warnings.Count > 0)
            {
                sb.AppendLine($"\n── {warnings.Count} WARNING(S) ──");
                foreach (var w in warnings) sb.AppendLine("  ⚠ " + w);
            }
            if (errors.Count == 0 && warnings.Count == 0)
                sb.AppendLine("✓ All cards passed validation.");

            Debug.Log(sb.ToString());

            // Show summary dialog so non-coders see it without opening Console
            string title   = errors.Count > 0 ? "Validation — Errors Found" : "Validation — Passed";
            string message = errors.Count > 0
                ? $"{errors.Count} error(s) and {warnings.Count} warning(s) found.\nSee Console for details."
                : warnings.Count > 0
                    ? $"No errors, but {warnings.Count} warning(s). See Console for details."
                    : "All cards passed validation. Ready to build.";

            EditorUtility.DisplayDialog(title, message, "OK");
        }

        [MenuItem("ThroneOfTides/Find Cards Missing Art  %#f")]  // Ctrl+Shift+F
        public static void FindCardsMissingArt()
        {
            var cards   = LoadAll<CardSO>();
            var missing = cards.FindAll(c => c.Art == null);

            if (missing.Count == 0)
            {
                EditorUtility.DisplayDialog("Missing Art", "All cards have art assigned. 🎉", "OK");
                return;
            }

            var sb = new StringBuilder();
            sb.AppendLine($"{missing.Count} card(s) are missing art:\n");
            foreach (var c in missing)
                sb.AppendLine($"  • {c.Name}  ({AssetDatabase.GetAssetPath(c)})");

            Debug.LogWarning(sb.ToString());

            // Ping the first offender in the Project window so it can be located fast
            EditorGUIUtility.PingObject(missing[0]);
            Selection.objects = missing.ConvertAll(c => (Object)c).ToArray();

            EditorUtility.DisplayDialog(
                "Missing Art",
                $"{missing.Count} card(s) have no art.\nThey are now selected in the Project window.\nSee Console for the full list.",
                "OK");
        }

        // Only available in Play Mode — simulates dealing a starting hand to the Console
        [MenuItem("ThroneOfTides/Log Opening Hand (Play Mode)  %#h", true)]
        private static bool LogOpeningHandValidate() => Application.isPlaying;

        [MenuItem("ThroneOfTides/Log Opening Hand (Play Mode)  %#h")]
        public static void LogOpeningHand()
        {
            var decks = LoadAll<DeckDefinitionSO>();
            if (decks.Count == 0)
            {
                Debug.LogWarning("No DeckDefinitionSO assets found.");
                return;
            }

            var deck  = decks.Find(d => d.name.Contains("Player")) ?? decks[0];
            var built = deck.BuildDeck();

            for (int i = built.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                (built[i], built[j]) = (built[j], built[i]);
            }

            var hand = built.GetRange(0, Mathf.Min(5, built.Count));
            var sb   = new StringBuilder();
            sb.AppendLine($"Opening hand from [{deck.name}]:");
            foreach (var c in hand)
                sb.AppendLine($"  • {c.Name} ({c.CardType}, {c.Damage} dmg)");
            Debug.Log(sb.ToString());
        }

        // ── Canonical Validation ────────────────────────────────────────────────

        // Single source of truth for all card and deck validation rules.
        // Both the build pipeline and the menu-item validator delegate here, ensuring
        // that adding a new card type or rule only requires a change in one place.
        public static (List<string> errors, List<string> warnings) ValidateAllData()
        {
            var errors   = new List<string>();
            var warnings = new List<string>();

            ValidateCards(errors, warnings);
            ValidateDecks(errors, warnings);

            return (errors, warnings);
        }

        private static void ValidateCards(List<string> errors, List<string> warnings)
        {
            foreach (var card in LoadAll<CardSO>())
            {
                if (string.IsNullOrWhiteSpace(card.Name))
                    errors.Add($"Card asset '{card.name}' has no name.");

                if (card.CardType == Core.CardType.Action && card.ActionEffect == null)
                    errors.Add($"'{card.Name}' is an Action card with no Effect SO.");

                if (card.CardType == Core.CardType.Combo && card.ComboPartner == null)
                    errors.Add($"'{card.Name}' is a Combo card with no partner.");

                if (card.Art == null)
                    warnings.Add($"'{card.Name}' has no art sprite.");

                if (string.IsNullOrWhiteSpace(card.Description))
                    warnings.Add($"'{card.Name}' has no description.");

                if (card.CardType == Core.CardType.DOT &&
                    (card.DotDamagePerTurn == 0 || card.DotDuration == 0))
                    warnings.Add($"'{card.Name}' is a DOT card with zero damage or duration.");
            }
        }

        private static void ValidateDecks(List<string> errors, List<string> warnings)
        {
            foreach (var deck in LoadAll<DeckDefinitionSO>())
            {
                foreach (var entry in deck.Cards)
                    if (entry.Card == null)
                        errors.Add($"Deck '{deck.name}' has a null card entry.");

                int total = deck.BuildDeck().Count;
                if (total == 0)       errors.Add($"Deck '{deck.name}' is empty.");
                else if (total < 10)  warnings.Add($"Deck '{deck.name}' has only {total} cards.");
            }
        }

        // ── Reusable Search Utility ─────────────────────────────────────────────

        // Returns all assets of type T in the project — used by inspectors and build pipeline
        public static List<T> LoadAll<T>() where T : ScriptableObject
        {
            var results = new List<T>();
            var guids   = AssetDatabase.FindAssets($"t:{typeof(T).Name}");
            foreach (var guid in guids)
            {
                var path  = AssetDatabase.GUIDToAssetPath(guid);
                var asset = AssetDatabase.LoadAssetAtPath<T>(path);
                if (asset != null) results.Add(asset);
            }
            return results;
        }

        // Finds all DeckDefinitionSOs that contain a specific card — used by deletion dialog
        public static List<DeckDefinitionSO> FindDecksContaining(CardSO card)
        {
            var decks  = LoadAll<DeckDefinitionSO>();
            var result = new List<DeckDefinitionSO>();
            foreach (var deck in decks)
                foreach (var entry in deck.Cards)
                    if (entry.Card == card) { result.Add(deck); break; }
            return result;
        }
    }
}