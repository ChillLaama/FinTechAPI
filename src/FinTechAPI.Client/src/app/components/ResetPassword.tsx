import { useState } from "react";
import { useNavigate } from "react-router";
import { Eye, EyeOff, Lock, CheckCircle2, AlertCircle } from "lucide-react";
import { Button } from "./ui/button";
import { Input } from "./ui/input";
import { Label } from "./ui/label";

export function ResetPassword() {
  const navigate = useNavigate();
  const [showPassword, setShowPassword] = useState(false);
  const [showConfirmPassword, setShowConfirmPassword] = useState(false);
  const [password, setPassword] = useState("");
  const [confirmPassword, setConfirmPassword] = useState("");
  const [isLoading, setIsLoading] = useState(false);
  const [errors, setErrors] = useState<string[]>([]);

  const passwordRequirements = [
    { label: "At least 8 characters", test: (p: string) => p.length >= 8 },
    { label: "Contains numbers", test: (p: string) => /\d/.test(p) },
    {
      label: "Contains uppercase letters",
      test: (p: string) => /[A-Z]/.test(p),
    },
    {
      label: "Contains special characters",
      test: (p: string) => /[!@#$%^&*(),.?":{}|<>]/.test(p),
    },
  ];

  const validatePassword = () => {
    const newErrors: string[] = [];

    passwordRequirements.forEach((req) => {
      if (!req.test(password)) {
        newErrors.push(req.label);
      }
    });

    if (password !== confirmPassword) {
      newErrors.push("Passwords do not match");
    }

    return newErrors;
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    const validationErrors = validatePassword();

    if (validationErrors.length > 0) {
      setErrors(validationErrors);
      return;
    }

    setErrors([]);
    setIsLoading(true);

    // Simulate password reset
    setTimeout(() => {
      navigate("/login");
    }, 1500);
  };

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="text-center space-y-2">
        <h1 className="text-3xl text-foreground">New password</h1>
        <p className="text-muted-foreground">
          Create a strong password for your account
        </p>
      </div>

      {/* Form */}
      <form onSubmit={handleSubmit} className="space-y-4">
        {errors.length > 0 && (
          <div className="bg-destructive/10 border border-destructive/30 rounded-lg p-4">
            <div className="flex items-start gap-3 mb-2">
              <AlertCircle className="w-5 h-5 text-destructive flex-shrink-0 mt-0.5" />
              <p className="text-sm text-destructive">
                Please fix the following errors:
              </p>
            </div>
            <ul className="list-disc list-inside space-y-1 ml-8">
              {errors.map((error, index) => (
                <li key={index} className="text-sm text-destructive">
                  {error}
                </li>
              ))}
            </ul>
          </div>
        )}

        <div className="space-y-2">
          <Label htmlFor="password">New password</Label>
          <div className="relative">
            <Lock className="absolute left-3 top-1/2 -translate-y-1/2 w-5 h-5 text-muted-foreground" />
            <Input
              id="password"
              type={showPassword ? "text" : "password"}
              placeholder="••••••••"
              value={password}
              onChange={(e) => setPassword(e.target.value)}
              className="pl-10 pr-10 bg-input-background"
              required
            />
            <button
              type="button"
              onClick={() => setShowPassword(!showPassword)}
              className="absolute right-3 top-1/2 -translate-y-1/2 text-muted-foreground hover:text-foreground transition-colors"
            >
              {showPassword ? (
                <EyeOff className="w-5 h-5" />
              ) : (
                <Eye className="w-5 h-5" />
              )}
            </button>
          </div>
        </div>

        <div className="space-y-2">
          <Label htmlFor="confirmPassword">Confirm password</Label>
          <div className="relative">
            <Lock className="absolute left-3 top-1/2 -translate-y-1/2 w-5 h-5 text-muted-foreground" />
            <Input
              id="confirmPassword"
              type={showConfirmPassword ? "text" : "password"}
              placeholder="••••••••"
              value={confirmPassword}
              onChange={(e) => setConfirmPassword(e.target.value)}
              className="pl-10 pr-10 bg-input-background"
              required
            />
            <button
              type="button"
              onClick={() => setShowConfirmPassword(!showConfirmPassword)}
              className="absolute right-3 top-1/2 -translate-y-1/2 text-muted-foreground hover:text-foreground transition-colors"
            >
              {showConfirmPassword ? (
                <EyeOff className="w-5 h-5" />
              ) : (
                <Eye className="w-5 h-5" />
              )}
            </button>
          </div>
        </div>

        {/* Password Requirements */}
        <div className="bg-card border border-border rounded-lg p-4 space-y-3">
          <p className="text-sm text-foreground">Password requirements:</p>
          <ul className="space-y-2">
            {passwordRequirements.map((req, index) => {
              const isValid = req.test(password);
              return (
                <li
                  key={index}
                  className={`flex items-center gap-2 text-sm transition-colors ${
                    password.length > 0
                      ? isValid
                        ? "text-success"
                        : "text-destructive"
                      : "text-muted-foreground"
                  }`}
                >
                  {password.length > 0 && isValid ? (
                    <CheckCircle2 className="w-4 h-4" />
                  ) : (
                    <div className="w-4 h-4 rounded-full border-2 border-current" />
                  )}
                  {req.label}
                </li>
              );
            })}
          </ul>
        </div>

        <Button type="submit" className="w-full" disabled={isLoading}>
          {isLoading ? "Saving..." : "Save password"}
        </Button>
      </form>

      {/* Security note */}
      <div className="bg-accent/10 border border-accent/30 rounded-lg p-4">
        <p className="text-sm text-accent-foreground/90">
          <strong>Security tip:</strong> Use a unique password that you don’t
          reuse on other websites. We recommend using a password manager.
        </p>
      </div>
    </div>
  );
}
