﻿using System.Collections.Generic;
using System.Linq;
using Dietphone.Models;

namespace Dietphone.ViewModels
{
    public class ReplacementItemViewModel
    {
        public ReplacementItem ReplacementItem { get; private set; }
        public PatternViewModel Pattern { get; private set; }
        public IList<PatternViewModel> Alternatives { get; private set; }
        public bool HasAlternatives { get; private set; }

        public ReplacementItemViewModel(ReplacementItem replacementItem, Factories factories,
            IList<InsulinCircumstanceViewModel> allCircumstances)
        {
            ReplacementItem = replacementItem;
            Pattern = new PatternViewModel(replacementItem.Pattern, factories, allCircumstances: allCircumstances);
            Alternatives = replacementItem.Alternatives
                .Select(pattern => new PatternViewModel(pattern, factories, allCircumstances: allCircumstances))
                .ToList();
            HasAlternatives = Alternatives.Any();
        }
    }
}