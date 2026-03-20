import { useState, useEffect } from "react";
import { Link, useNavigate, useSearchParams } from "react-router";
import {
  Mail,
  CheckCircle2,
  Clock,
  RefreshCw,
  AlertCircle,
} from "lucide-react";
import { Button } from "./ui/button";
import { sendVerificationEmail, verifyEmail } from "../api/client";

export function VerifyEmail() {
  const navigate = useNavigate();
  const [searchParams] = useSearchParams();
  const [oobCode, setOobCode] = useState("");
  const [isVerifying, setIsVerifying] = useState(false);
  const [isVerified, setIsVerified] = useState(false);
  const [error, setError] = useState("");
  const [countdown, setCountdown] = useState(60);
  const [canResend, setCanResend] = useState(false);

  useEffect(() => {
    const code = searchParams.get("oobCode");
    if (code) {
      setOobCode(code);
    }
  }, [searchParams]);

  useEffect(() => {
    if (countdown > 0 && !canResend) {
      const timer = setTimeout(() => setCountdown(countdown - 1), 1000);
      return () => clearTimeout(timer);
    } else {
      setCanResend(true);
    }
  }, [countdown, canResend]);

  const handleResend = async () => {
    setError("");

    try {
      await sendVerificationEmail();
      setCountdown(60);
      setCanResend(false);
    } catch (requestError) {
      setError(
        requestError instanceof Error
          ? requestError.message
          : "Failed to resend verification email",
      );
    }
  };

  const handleVerify = async () => {
    if (!oobCode.trim()) {
      setError("Verification code is required");
      return;
    }

    setIsVerifying(true);
    setError("");

    try {
      await verifyEmail(oobCode.trim());
      setIsVerifying(false);
      setIsVerified(true);
      setTimeout(() => {
        navigate("/login");
      }, 2000);
    } catch (requestError) {
      setIsVerifying(false);
      setError(
        requestError instanceof Error
          ? requestError.message
          : "Failed to verify email",
      );
    }
  };

  if (isVerified) {
    return (
      <div className="space-y-6 text-center">
        <div className="inline-flex items-center justify-center w-20 h-20 rounded-full bg-success/10 border border-success/30 animate-pulse">
          <CheckCircle2 className="w-10 h-10 text-success" />
        </div>

        <div className="space-y-2">
          <h1 className="text-3xl text-foreground">Email verified!</h1>
          <p className="text-muted-foreground">
            Your account has been activated. Redirecting to sign in...
          </p>
        </div>
      </div>
    );
  }

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="text-center space-y-4">
        <div className="inline-flex items-center justify-center w-16 h-16 rounded-full bg-accent/10 border border-accent/30">
          <Mail className="w-8 h-8 text-accent" />
        </div>

        <div className="space-y-2">
          <h1 className="text-3xl text-foreground">Verify your email</h1>
          <p className="text-muted-foreground">
            We sent a verification code to
          </p>
          <p className="text-foreground">demo@financehub.com</p>
        </div>
      </div>

      {/* Verification Code Input */}
      <div className="space-y-4">
        {error && (
          <div className="bg-destructive/10 border border-destructive/30 rounded-lg p-4 flex items-start gap-3">
            <AlertCircle className="w-5 h-5 text-destructive flex-shrink-0 mt-0.5" />
            <p className="text-sm text-destructive">{error}</p>
          </div>
        )}

        <input
          type="text"
          value={oobCode}
          onChange={(event) => setOobCode(event.target.value)}
          className="w-full h-12 px-4 bg-input-background border border-input rounded-lg focus:border-primary focus:ring-2 focus:ring-primary/20 outline-none transition-all"
          placeholder="Paste verification code from email link"
        />

        <Button
          className="w-full"
          onClick={handleVerify}
          disabled={isVerifying}
        >
          {isVerifying ? "Verifying..." : "Verify"}
        </Button>
      </div>

      {/* Resend */}
      <div className="text-center space-y-3">
        <p className="text-sm text-muted-foreground">
          Didn’t receive the code?
        </p>

        {canResend ? (
          <Button variant="outline" onClick={handleResend} className="w-full">
            <RefreshCw className="w-4 h-4 mr-2" />
            Resend code
          </Button>
        ) : (
          <div className="flex items-center justify-center gap-2 text-sm text-muted-foreground">
            <Clock className="w-4 h-4" />
            <span>Resend in {countdown}s</span>
          </div>
        )}
      </div>

      {/* Info */}
      <div className="bg-card border border-border rounded-lg p-4 space-y-2">
        <p className="text-sm text-muted-foreground">
          <strong className="text-foreground">Tips:</strong>
        </p>
        <ul className="text-sm text-muted-foreground space-y-1 list-disc list-inside ml-2">
          <li>Check your spam folder</li>
          <li>Make sure your email address is correct</li>
          <li>The code is valid for 15 minutes</li>
        </ul>
      </div>

      {/* Back to login */}
      <div className="text-center pt-4 border-t border-border">
        <Link
          to="/login"
          className="text-sm text-accent hover:text-accent/80 transition-colors"
        >
          Back to sign in
        </Link>
      </div>
    </div>
  );
}
