import { useCallback, useEffect, useState } from "react";
import { useNavigate, useParams } from "react-router";
import {
  Shield,
  AlertTriangle,
  CheckCircle,
  XCircle,
  ArrowLeft,
  Loader2,
  ArrowUpRight,
  UserCheck,
} from "lucide-react";
import {
  getFraudCaseById,
  getFraudCaseEvaluation,
  approveFraudCase,
  rejectFraudCase,
  escalateFraudCase,
  assignFraudCase,
} from "../api/client";
import type { ApiFraudCase, ApiFraudEvaluation } from "../api/client";

function statusColor(status: string): string {
  switch (status.toLowerCase()) {
    case "open":
      return "text-yellow-500 bg-yellow-500/10 border-yellow-500/20";
    case "inreview":
      return "text-blue-500 bg-blue-500/10 border-blue-500/20";
    case "approved":
      return "text-green-500 bg-green-500/10 border-green-500/20";
    case "rejected":
      return "text-red-500 bg-red-500/10 border-red-500/20";
    default:
      return "text-muted-foreground bg-secondary border-border";
  }
}

function riskColor(level: string): string {
  switch (level.toLowerCase()) {
    case "critical":
      return "text-red-600";
    case "high":
      return "text-red-500";
    case "medium":
      return "text-yellow-500";
    case "low":
      return "text-green-500";
    default:
      return "text-muted-foreground";
  }
}

function formatAmount(minorUnits: number, currency: string): string {
  const major = minorUnits / 100;
  return `${currency.toUpperCase()} ${major.toLocaleString("en-US", {
    minimumFractionDigits: 2,
    maximumFractionDigits: 2,
  })}`;
}

export function FraudCaseDetails() {
  const { caseId } = useParams<{ caseId: string }>();
  const navigate = useNavigate();

  const [fraudCase, setFraudCase] = useState<ApiFraudCase | null>(null);
  const [evaluation, setEvaluation] = useState<ApiFraudEvaluation | null>(null);
  const [loading, setLoading] = useState(true);
  const [actionLoading, setActionLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [notes, setNotes] = useState("");
  const [assigneeInput, setAssigneeInput] = useState("");

  const loadCase = useCallback(async () => {
    if (!caseId) return;
    try {
      setLoading(true);
      setError(null);
      const [c, e] = await Promise.all([
        getFraudCaseById(caseId),
        getFraudCaseEvaluation(caseId).catch(() => null),
      ]);
      setFraudCase(c);
      setEvaluation(e);
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed to load case");
    } finally {
      setLoading(false);
    }
  }, [caseId]);

  useEffect(() => {
    loadCase();
  }, [loadCase]);

  const handleAction = async (action: "approve" | "reject" | "escalate") => {
    if (!caseId) return;
    try {
      setActionLoading(true);
      setError(null);
      let updated: ApiFraudCase;
      switch (action) {
        case "approve":
          updated = await approveFraudCase(caseId, notes || undefined);
          break;
        case "reject":
          updated = await rejectFraudCase(caseId, notes || undefined);
          break;
        case "escalate":
          updated = await escalateFraudCase(caseId, notes || undefined);
          break;
      }
      setFraudCase(updated);
      setNotes("");
    } catch (err) {
      setError(err instanceof Error ? err.message : `Failed to ${action} case`);
    } finally {
      setActionLoading(false);
    }
  };

  const handleAssign = async () => {
    if (!caseId || !assigneeInput.trim()) return;
    try {
      setActionLoading(true);
      setError(null);
      const updated = await assignFraudCase(caseId, assigneeInput.trim());
      setFraudCase(updated);
      setAssigneeInput("");
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed to assign case");
    } finally {
      setActionLoading(false);
    }
  };

  const isTerminal =
    fraudCase?.status.toLowerCase() === "approved" ||
    fraudCase?.status.toLowerCase() === "rejected" ||
    fraudCase?.status.toLowerCase() === "expired";

  if (loading) {
    return (
      <div className="flex justify-center py-20">
        <Loader2 className="w-8 h-8 animate-spin text-accent" />
      </div>
    );
  }

  if (!fraudCase) {
    return (
      <div className="text-center py-20 space-y-4">
        <XCircle className="w-12 h-12 mx-auto text-destructive" />
        <p className="text-muted-foreground">Case not found</p>
        <button
          onClick={() => navigate("/fraud-cases")}
          className="text-accent hover:underline text-sm"
        >
          Back to cases
        </button>
      </div>
    );
  }

  return (
    <div className="max-w-3xl mx-auto space-y-6">
      {/* Header */}
      <div className="flex items-center gap-4">
        <button
          onClick={() => navigate("/fraud-cases")}
          className="p-2 rounded-lg hover:bg-secondary transition-colors"
        >
          <ArrowLeft className="w-5 h-5" />
        </button>
        <div className="flex-1">
          <div className="flex items-center gap-3 mb-1">
            <h1 className="text-2xl">Fraud case</h1>
            <span
              className={`text-xs px-2 py-0.5 rounded-full font-medium border ${statusColor(fraudCase.status)}`}
            >
              {fraudCase.status}
            </span>
          </div>
          <p className="text-xs text-muted-foreground font-mono">
            {fraudCase.id}
          </p>
        </div>
      </div>

      {error && (
        <div className="flex items-start gap-3 p-4 bg-destructive/10 border border-destructive/20 rounded-lg">
          <AlertTriangle className="w-5 h-5 text-destructive flex-shrink-0 mt-0.5" />
          <p className="text-sm">{error}</p>
        </div>
      )}

      {/* Overview */}
      <div className="bg-card p-6 rounded-xl border border-border space-y-4">
        <h2 className="text-lg font-medium flex items-center gap-2">
          <Shield className="w-5 h-5 text-accent" /> Overview
        </h2>
        <div className="grid grid-cols-2 gap-4 text-sm">
          <div>
            <span className="text-muted-foreground">Amount</span>
            <p className="font-medium">
              {formatAmount(fraudCase.amountMinorUnits, fraudCase.currency)}
            </p>
          </div>
          <div>
            <span className="text-muted-foreground">Fraud score</span>
            <p className="font-medium">{fraudCase.fraudScore} / 100</p>
          </div>
          <div>
            <span className="text-muted-foreground">Risk level</span>
            <p className={`font-medium ${riskColor(fraudCase.riskLevel)}`}>
              {fraudCase.riskLevel}
            </p>
          </div>
          <div>
            <span className="text-muted-foreground">User ID</span>
            <p className="font-mono text-xs">{fraudCase.userId}</p>
          </div>
          {fraudCase.paymentId && (
            <div>
              <span className="text-muted-foreground">Payment ID</span>
              <p className="font-mono text-xs">{fraudCase.paymentId}</p>
            </div>
          )}
          {fraudCase.assignee && (
            <div>
              <span className="text-muted-foreground">Assignee</span>
              <p className="text-xs">{fraudCase.assignee}</p>
            </div>
          )}
          {fraudCase.resolvedBy && (
            <div>
              <span className="text-muted-foreground">Resolved by</span>
              <p className="text-xs">{fraudCase.resolvedBy}</p>
            </div>
          )}
          {fraudCase.resolvedAt && (
            <div>
              <span className="text-muted-foreground">Resolved at</span>
              <p className="text-xs">
                {new Date(fraudCase.resolvedAt).toLocaleString()}
              </p>
            </div>
          )}
          <div>
            <span className="text-muted-foreground">Created</span>
            <p className="text-xs">
              {new Date(fraudCase.createdAt).toLocaleString()}
            </p>
          </div>
          <div>
            <span className="text-muted-foreground">Updated</span>
            <p className="text-xs">
              {new Date(fraudCase.updatedAt).toLocaleString()}
            </p>
          </div>
        </div>
      </div>

      {/* Triggered rules */}
      {fraudCase.rulesTriggered.length > 0 && (
        <div className="bg-card p-6 rounded-xl border border-border space-y-3">
          <h2 className="text-lg font-medium">Triggered rules</h2>
          <div className="flex gap-2 flex-wrap">
            {fraudCase.rulesTriggered.map((rule) => (
              <span
                key={rule}
                className="text-xs px-2 py-1 bg-red-500/10 text-red-500 rounded-full"
              >
                {rule}
              </span>
            ))}
          </div>
          {fraudCase.reasons.length > 0 && (
            <ul className="space-y-1 text-sm text-muted-foreground">
              {fraudCase.reasons.map((reason, i) => (
                <li key={i} className="flex items-start gap-2">
                  <AlertTriangle className="w-3.5 h-3.5 mt-0.5 flex-shrink-0 text-yellow-500" />
                  {reason}
                </li>
              ))}
            </ul>
          )}
        </div>
      )}

      {/* Evaluation details */}
      {evaluation && (
        <div className="bg-card p-6 rounded-xl border border-border space-y-3">
          <h2 className="text-lg font-medium">Evaluation details</h2>
          <div className="grid grid-cols-2 gap-3 text-sm">
            <div>
              <span className="text-muted-foreground">Evaluation ID</span>
              <p className="font-mono text-xs">{evaluation.id}</p>
            </div>
            <div>
              <span className="text-muted-foreground">Decision</span>
              <p className="font-medium">{evaluation.decision}</p>
            </div>
            <div>
              <span className="text-muted-foreground">Rules version</span>
              <p>{evaluation.rulesVersion}</p>
            </div>
            <div>
              <span className="text-muted-foreground">Evaluated at</span>
              <p className="text-xs">
                {new Date(evaluation.createdAt).toLocaleString()}
              </p>
            </div>
          </div>
        </div>
      )}

      {/* Analyst notes */}
      {fraudCase.analystNotes && (
        <div className="bg-card p-6 rounded-xl border border-border space-y-2">
          <h2 className="text-lg font-medium">Analyst notes</h2>
          <p className="text-sm text-muted-foreground whitespace-pre-wrap">
            {fraudCase.analystNotes}
          </p>
        </div>
      )}

      {/* Actions */}
      {!isTerminal && (
        <div className="bg-card p-6 rounded-xl border border-border space-y-4">
          <h2 className="text-lg font-medium">Actions</h2>

          <div>
            <label className="block text-sm text-muted-foreground mb-1">
              Notes (optional)
            </label>
            <textarea
              rows={2}
              value={notes}
              onChange={(e) => setNotes(e.target.value)}
              className="w-full px-3 py-2 bg-input-background border border-input rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-ring resize-none"
              placeholder="Add analyst notes..."
            />
          </div>

          <div className="flex gap-3 flex-wrap">
            <button
              onClick={() => handleAction("approve")}
              disabled={actionLoading}
              className="flex items-center gap-2 px-4 py-2 bg-green-600 text-white rounded-lg hover:bg-green-700 transition-colors text-sm disabled:opacity-60"
            >
              <CheckCircle className="w-4 h-4" /> Approve
            </button>
            <button
              onClick={() => handleAction("reject")}
              disabled={actionLoading}
              className="flex items-center gap-2 px-4 py-2 bg-red-600 text-white rounded-lg hover:bg-red-700 transition-colors text-sm disabled:opacity-60"
            >
              <XCircle className="w-4 h-4" /> Reject
            </button>
            <button
              onClick={() => handleAction("escalate")}
              disabled={actionLoading}
              className="flex items-center gap-2 px-4 py-2 bg-blue-600 text-white rounded-lg hover:bg-blue-700 transition-colors text-sm disabled:opacity-60"
            >
              <ArrowUpRight className="w-4 h-4" /> Escalate
            </button>
          </div>

          <div className="border-t border-border pt-4">
            <label className="block text-sm text-muted-foreground mb-1">
              Assign to
            </label>
            <div className="flex gap-2">
              <input
                type="text"
                value={assigneeInput}
                onChange={(e) => setAssigneeInput(e.target.value)}
                className="flex-1 px-3 py-2 bg-input-background border border-input rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-ring"
                placeholder="analyst@example.com"
              />
              <button
                onClick={handleAssign}
                disabled={actionLoading || !assigneeInput.trim()}
                className="flex items-center gap-2 px-4 py-2 bg-accent text-accent-foreground rounded-lg hover:bg-accent/90 transition-colors text-sm disabled:opacity-60"
              >
                <UserCheck className="w-4 h-4" /> Assign
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}
