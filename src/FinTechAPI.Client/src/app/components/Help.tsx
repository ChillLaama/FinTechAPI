import { Link } from "react-router";
import { ArrowLeft, Mail, MessageCircle } from "lucide-react";

export function Help() {
  return (
    <div className="space-y-6">
      <Link
        to="/"
        className="inline-flex items-center gap-1 text-sm text-accent hover:text-accent/80"
      >
        <ArrowLeft className="w-4 h-4" />
        Back
      </Link>

      <h1 className="text-3xl text-foreground">Help Center</h1>

      <div className="space-y-4 text-muted-foreground">
        <p>
          Need assistance? Here are the most common ways to get help with
          FinanceHub.
        </p>

        <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
          <div className="bg-card border border-border rounded-xl p-5 space-y-2">
            <div className="flex items-center gap-2 text-foreground">
              <Mail className="w-5 h-5 text-accent" />
              <h2 className="text-lg">Email Support</h2>
            </div>
            <p className="text-sm">
              Send us an email at{" "}
              <span className="text-accent">support@financehub.com</span> and
              we&apos;ll respond within 24 hours.
            </p>
          </div>

          <div className="bg-card border border-border rounded-xl p-5 space-y-2">
            <div className="flex items-center gap-2 text-foreground">
              <MessageCircle className="w-5 h-5 text-accent" />
              <h2 className="text-lg">FAQ</h2>
            </div>
            <ul className="text-sm space-y-1 list-disc list-inside">
              <li>Payments typically settle within 2 business days.</li>
              <li>Payouts are processed from the platform Stripe balance.</li>
              <li>Contact support to enable two-factor authentication.</li>
            </ul>
          </div>
        </div>
      </div>
    </div>
  );
}
