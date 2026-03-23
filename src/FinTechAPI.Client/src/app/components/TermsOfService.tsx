import { Link } from "react-router";
import { ArrowLeft } from "lucide-react";

export function TermsOfService() {
  return (
    <div className="space-y-6">
      <Link
        to="/register"
        className="inline-flex items-center gap-1 text-sm text-accent hover:text-accent/80"
      >
        <ArrowLeft className="w-4 h-4" />
        Back
      </Link>

      <h1 className="text-3xl text-foreground">Terms of Service</h1>

      <div className="prose prose-sm text-muted-foreground space-y-4">
        <p>
          These Terms of Service govern your use of the FinanceHub platform. By
          creating an account, you agree to these terms.
        </p>

        <h2 className="text-lg text-foreground">1. Account Responsibilities</h2>
        <p>
          You are responsible for maintaining the confidentiality of your
          account credentials and for all activity that occurs under your
          account.
        </p>

        <h2 className="text-lg text-foreground">2. Acceptable Use</h2>
        <p>
          You agree not to use the platform for any illegal or unauthorized
          purpose, including money laundering, fraud, or any activity that
          violates applicable financial regulations.
        </p>

        <h2 className="text-lg text-foreground">3. Payment Processing</h2>
        <p>
          Payment processing is provided through Stripe. All transactions are
          subject to Stripe&apos;s terms of service and applicable fees.
        </p>

        <h2 className="text-lg text-foreground">4. Limitation of Liability</h2>
        <p>
          FinanceHub is provided &quot;as is&quot; without warranty of any kind.
          We are not liable for any indirect, incidental, or consequential
          damages.
        </p>

        <p className="text-xs text-muted-foreground/60">
          Last updated: March 2026
        </p>
      </div>
    </div>
  );
}
