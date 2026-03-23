import { Link } from "react-router";
import { ArrowLeft } from "lucide-react";

export function PrivacyPolicy() {
  return (
    <div className="space-y-6">
      <Link
        to="/register"
        className="inline-flex items-center gap-1 text-sm text-accent hover:text-accent/80"
      >
        <ArrowLeft className="w-4 h-4" />
        Back
      </Link>

      <h1 className="text-3xl text-foreground">Privacy Policy</h1>

      <div className="prose prose-sm text-muted-foreground space-y-4">
        <p>
          FinanceHub is committed to protecting your personal information. This
          policy describes how we collect, use, and safeguard your data.
        </p>

        <h2 className="text-lg text-foreground">1. Data We Collect</h2>
        <p>
          We collect account information (name, email), transaction data, and
          usage analytics necessary to provide our services.
        </p>

        <h2 className="text-lg text-foreground">2. How We Use Your Data</h2>
        <p>
          Your data is used to process payments, display transaction history,
          prevent fraud, and improve the platform. We do not sell your data to
          third parties.
        </p>

        <h2 className="text-lg text-foreground">3. Data Storage</h2>
        <p>
          Data is stored securely in Google Cloud Firestore with
          encryption at rest. Payment data is processed by Stripe and is
          not stored on our servers.
        </p>

        <h2 className="text-lg text-foreground">4. Your Rights</h2>
        <p>
          You may request access to, correction of, or deletion of your
          personal data at any time by contacting support or through your
          account settings.
        </p>

        <p className="text-xs text-muted-foreground/60">
          Last updated: March 2026
        </p>
      </div>
    </div>
  );
}
