import { useState } from "react";
import { useMutation } from "@tanstack/react-query";
import { ArrowUpRight, Footprints } from "lucide-react";
import { api } from "../api/citySurfersApi";
import type { LoginResponse } from "../api/types";
import { errorMessage } from "../api/errors";
import { Button } from "../components/ui";
export default function LoginPage({
  onLogin,
}: {
  onLogin: (identity: LoginResponse) => void;
}) {
  const [username, setUsername] = useState("demo");
  const [password, setPassword] = useState("1234");
  const login = useMutation({
    mutationFn: api.login,
    onSuccess: (identity) => onLogin(identity),
    retry: false,
  });
  return (
    <main className="login">
      <div className="wordmark">
        <Footprints />
        CITYSURFERS
        <span className="brand-dot" />
      </div>
      <div className="login-art" aria-hidden="true">
        <span>KRK</span>
        <i />
        <i />
        <i />
      </div>
      <span className="eyebrow accent">YOUR CITY. YOUR NEXT MOVE.</span>
      <h1>
        Every run.
        <br />A little <em>higher.</em>
      </h1>
      <p className="intro">
        Turn your next kilometre into a comeback.
        <br />
        Catch a runner. Climb Kraków.
      </p>
      <form
        onSubmit={(event) => {
          event.preventDefault();
          if (!login.isPending) login.mutate({ username, password });
        }}
      >
        <label>
          Username
          <input
            autoComplete="username"
            value={username}
            onChange={(e) => setUsername(e.target.value)}
            required
          />
        </label>
        <label>
          Password
          <input
            type="password"
            autoComplete="current-password"
            value={password}
            onChange={(e) => setPassword(e.target.value)}
            required
          />
        </label>
        {login.isError && (
          <p role="alert" className="error-text">
            {errorMessage(login.error)}
          </p>
        )}
        <Button disabled={login.isPending} type="submit">
          {login.isPending ? "Entering the city…" : "Enter the city"}
          <ArrowUpRight size={20} />
        </Button>
      </form>
      <p className="demo-note">
        HACKATHON DEMO · KRAKÓW
        <br />
        <span>Fictional credentials. One shared demo city.</span>
      </p>
    </main>
  );
}
