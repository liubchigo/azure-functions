﻿namespace Functions.Model
{
    public class EvaluatedRule
    {
        public string Description { get; set; }
        public string Why { get; set; }
        public bool IsSox { get; set; }
        public bool Status { get; set; }
        public string Name { get; set; }
        public Reconcile Reconcile { get; set; }
    }
}