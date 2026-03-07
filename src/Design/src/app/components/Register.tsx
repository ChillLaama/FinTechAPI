import { useState } from "react";
import { Link, useNavigate } from "react-router";
import { Eye, EyeOff, AlertCircle, Mail, Lock, User, Phone } from "lucide-react";
import { Button } from "./ui/button";
import { Input } from "./ui/input";
import { Label } from "./ui/label";
import { Checkbox } from "./ui/checkbox";

export function Register() {
  const navigate = useNavigate();
  const [showPassword, setShowPassword] = useState(false);
  const [showConfirmPassword, setShowConfirmPassword] = useState(false);
  const [formData, setFormData] = useState({
    fullName: "",
    email: "",
    phone: "",
    password: "",
    confirmPassword: "",
    agreeToTerms: false,
  });
  const [errors, setErrors] = useState<string[]>([]);
  const [isLoading, setIsLoading] = useState(false);

  const handleChange = (field: string, value: string | boolean) => {
    setFormData((prev) => ({ ...prev, [field]: value }));
  };

  const validateForm = () => {
    const newErrors: string[] = [];

    if (formData.fullName.length < 2) {
      newErrors.push("Имя должно содержать минимум 2 символа");
    }

    if (!formData.email.includes("@")) {
      newErrors.push("Введите корректный email");
    }

    if (formData.phone.length < 10) {
      newErrors.push("Введите корректный номер телефона");
    }

    if (formData.password.length < 8) {
      newErrors.push("Пароль должен содержать минимум 8 символов");
    }

    if (formData.password !== formData.confirmPassword) {
      newErrors.push("Пароли не совпадают");
    }

    if (!formData.agreeToTerms) {
      newErrors.push("Необходимо принять условия использования");
    }

    return newErrors;
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    const validationErrors = validateForm();

    if (validationErrors.length > 0) {
      setErrors(validationErrors);
      return;
    }

    setErrors([]);
    setIsLoading(true);

    // Имитация регистрации
    setTimeout(() => {
      navigate("/verify-email");
    }, 1500);
  };

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="text-center space-y-2">
        <h1 className="text-3xl text-foreground">Регистрация</h1>
        <p className="text-muted-foreground">
          Создайте аккаунт для доступа к FinanceHub
        </p>
      </div>

      {/* Form */}
      <form onSubmit={handleSubmit} className="space-y-4">
        {errors.length > 0 && (
          <div className="bg-destructive/10 border border-destructive/30 rounded-lg p-4">
            <div className="flex items-start gap-3 mb-2">
              <AlertCircle className="w-5 h-5 text-destructive flex-shrink-0 mt-0.5" />
              <p className="text-sm text-destructive">
                Пожалуйста, исправьте следующие ошибки:
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
          <Label htmlFor="fullName">Полное имя</Label>
          <div className="relative">
            <User className="absolute left-3 top-1/2 -translate-y-1/2 w-5 h-5 text-muted-foreground" />
            <Input
              id="fullName"
              type="text"
              placeholder="Иван Петров"
              value={formData.fullName}
              onChange={(e) => handleChange("fullName", e.target.value)}
              className="pl-10 bg-input-background"
              required
            />
          </div>
        </div>

        <div className="space-y-2">
          <Label htmlFor="email">Email</Label>
          <div className="relative">
            <Mail className="absolute left-3 top-1/2 -translate-y-1/2 w-5 h-5 text-muted-foreground" />
            <Input
              id="email"
              type="email"
              placeholder="your@email.com"
              value={formData.email}
              onChange={(e) => handleChange("email", e.target.value)}
              className="pl-10 bg-input-background"
              required
            />
          </div>
        </div>

        <div className="space-y-2">
          <Label htmlFor="phone">Телефон</Label>
          <div className="relative">
            <Phone className="absolute left-3 top-1/2 -translate-y-1/2 w-5 h-5 text-muted-foreground" />
            <Input
              id="phone"
              type="tel"
              placeholder="+7 (999) 123-45-67"
              value={formData.phone}
              onChange={(e) => handleChange("phone", e.target.value)}
              className="pl-10 bg-input-background"
              required
            />
          </div>
        </div>

        <div className="space-y-2">
          <Label htmlFor="password">Пароль</Label>
          <div className="relative">
            <Lock className="absolute left-3 top-1/2 -translate-y-1/2 w-5 h-5 text-muted-foreground" />
            <Input
              id="password"
              type={showPassword ? "text" : "password"}
              placeholder="••••••••"
              value={formData.password}
              onChange={(e) => handleChange("password", e.target.value)}
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
          <p className="text-xs text-muted-foreground">
            Минимум 8 символов, включая цифры и специальные символы
          </p>
        </div>

        <div className="space-y-2">
          <Label htmlFor="confirmPassword">Подтвердите пароль</Label>
          <div className="relative">
            <Lock className="absolute left-3 top-1/2 -translate-y-1/2 w-5 h-5 text-muted-foreground" />
            <Input
              id="confirmPassword"
              type={showConfirmPassword ? "text" : "password"}
              placeholder="••••••••"
              value={formData.confirmPassword}
              onChange={(e) => handleChange("confirmPassword", e.target.value)}
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

        <div className="flex items-start gap-3 pt-2">
          <Checkbox
            id="terms"
            checked={formData.agreeToTerms}
            onCheckedChange={(checked) =>
              handleChange("agreeToTerms", checked === true)
            }
            className="mt-1"
          />
          <Label htmlFor="terms" className="text-sm cursor-pointer">
            Я принимаю{" "}
            <Link to="/terms" className="text-accent hover:text-accent/80">
              условия использования
            </Link>{" "}
            и{" "}
            <Link to="/privacy" className="text-accent hover:text-accent/80">
              политику конфиденциальности
            </Link>
          </Label>
        </div>

        <Button type="submit" className="w-full" disabled={isLoading}>
          {isLoading ? "Регистрация..." : "Зарегистрироваться"}
        </Button>
      </form>

      {/* Divider */}
      <div className="relative">
        <div className="absolute inset-0 flex items-center">
          <div className="w-full border-t border-border"></div>
        </div>
        <div className="relative flex justify-center text-sm">
          <span className="bg-background px-4 text-muted-foreground">Или</span>
        </div>
      </div>

      {/* Login link */}
      <div className="text-center">
        <p className="text-sm text-muted-foreground">
          Уже есть аккаунт?{" "}
          <Link
            to="/login"
            className="text-accent hover:text-accent/80 transition-colors"
          >
            Войти
          </Link>
        </p>
      </div>
    </div>
  );
}
