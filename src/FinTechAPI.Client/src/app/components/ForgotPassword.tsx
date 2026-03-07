import { useState } from "react";
import { Link } from "react-router";
import { ArrowLeft, Mail, CheckCircle2, AlertCircle } from "lucide-react";
import { Button } from "./ui/button";
import { Input } from "./ui/input";
import { Label } from "./ui/label";

export function ForgotPassword() {
  const [email, setEmail] = useState("");
  const [isLoading, setIsLoading] = useState(false);
  const [isSuccess, setIsSuccess] = useState(false);
  const [error, setError] = useState("");

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError("");
    setIsLoading(true);

    // Simulate email delivery
    setTimeout(() => {
      if (email.includes("@")) {
        setIsSuccess(true);
      } else {
        setError("Enter a valid email address");
      }
      setIsLoading(false);
    }, 1500);
  };

  if (isSuccess) {
    return (
      <div className="space-y-6">
        {/* Success state */}
        <div className="text-center space-y-4">
          <div className="inline-flex items-center justify-center w-16 h-16 rounded-full bg-success/10 border border-success/30">
            <CheckCircle2 className="w-8 h-8 text-success" />
          </div>
          
          <div className="space-y-2">
            <h1 className="text-3xl text-foreground">Check your email</h1>
            <p className="text-muted-foreground">
              We sent a password reset link to
            </p>
            <p className="text-foreground">{email}</p>
          </div>
        </div>

        <div className="bg-accent/10 border border-accent/30 rounded-lg p-4 space-y-3">
          <p className="text-sm text-accent-foreground/90">
            <strong>Didn't receive the email?</strong>
          </p>
          <ul className="text-sm text-muted-foreground space-y-2 list-disc list-inside ml-2">
            <li>Check your spam folder</li>
            <li>Make sure your email address is correct</li>
            <li>Wait a few minutes</li>
          </ul>
          <Button
            variant="outline"
            className="w-full mt-3"
            onClick={() => setIsSuccess(false)}
          >
            Resend
          </Button>
        </div>

        <div className="text-center">
          <Link
            to="/login"
            className="inline-flex items-center gap-2 text-sm text-accent hover:text-accent/80 transition-colors"
          >
            <ArrowLeft className="w-4 h-4" />
            Back to sign in
          </Link>
        </div>
      </div>
    );
  }

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="space-y-2">
        <Link
          to="/login"
          className="inline-flex items-center gap-2 text-sm text-muted-foreground hover:text-foreground transition-colors mb-4"
        >
          <ArrowLeft className="w-4 h-4" />
          Back to sign in
        </Link>
        
        <h1 className="text-3xl text-foreground">Forgot password?</h1>
        <p className="text-muted-foreground">
          Enter your email and we’ll send a password reset link
        </p>
      </div>

      {/* Form */}
      <form onSubmit={handleSubmit} className="space-y-4">
        {error && (
          <div className="bg-destructive/10 border border-destructive/30 rounded-lg p-4 flex items-start gap-3">
            <AlertCircle className="w-5 h-5 text-destructive flex-shrink-0 mt-0.5" />
            <p className="text-sm text-destructive">{error}</p>
          </div>
        )}

        <div className="space-y-2">
          <Label htmlFor="email">Email</Label>
          <div className="relative">
            <Mail className="absolute left-3 top-1/2 -translate-y-1/2 w-5 h-5 text-muted-foreground" />
            <Input
              id="email"
              type="email"
              placeholder="your@email.com"
              value={email}
              onChange={(e) => setEmail(e.target.value)}
              className="pl-10 bg-input-background"
              required
            />
          </div>
        </div>

        <Button type="submit" className="w-full" disabled={isLoading}>
          {isLoading ? "Sending..." : "Send link"}
        </Button>
      </form>

      {/* Info */}
      <div className="bg-card border border-border rounded-lg p-4">
        <p className="text-sm text-muted-foreground">
          You’ll have 24 hours to reset your password after receiving the link.
          If it expires, you can request a new one.
        </p>
      </div>
    </div>
  );
}
