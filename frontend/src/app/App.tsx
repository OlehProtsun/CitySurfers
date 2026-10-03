import { lazy, Suspense, useState, useLayoutEffect } from "react";
import {
  BrowserRouter,
  NavLink,
  Navigate,
  Outlet,
  Route,
  Routes,
  useNavigate,
  useLocation,
} from "react-router";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { MotionConfig } from "motion/react";
import {
  Footprints,
  Home,
  Trophy,
  Map,
  ChartNoAxesColumnIncreasing,
} from "lucide-react";
import { readIdentity, saveIdentity } from "./session";
import type { LoginResponse } from "../api/types";
import LoginPage from "../pages/LoginPage";
import { Loading } from "../components/ui";
import { RunProvider } from "../features/run/RunProvider";
import { OvertakeOverlay } from "../features/run/OvertakeOverlay";
const HomePage = lazy(() => import("../pages/HomePage"));
const RankingPage = lazy(() => import("../pages/RankingPage"));
const ActivityMapPage = lazy(() => import("../pages/ActivityMapPage"));
const ProgressPage = lazy(() => import("../pages/ProgressPage"));
const client = new QueryClient({
  defaultOptions: {
    queries: { staleTime: 10000, retry: 1 },
    mutations: { retry: false },
  },
});
function Shell({ identity }: { identity: LoginResponse }) {
  return (
    <RunProvider>
      <div className="app-shell">
        <header className="app-header">
          <span className="wordmark">
            <Footprints size={19} />
            CITYSURFERS
          </span>
          <span className="identity" title={identity.displayName}>
            {identity.displayName.slice(0, 2).toUpperCase()}
          </span>
        </header>
        <main className="main-content">
          <Suspense fallback={<Loading />}>
            <Outlet />
          </Suspense>
        </main>
        <nav className="bottom-nav" aria-label="Main navigation">
          {[
            { to: "/", label: "Home", icon: Home },
            { to: "/ranking", label: "Ranking", icon: Trophy },
            { to: "/map", label: "Map", icon: Map },
            {
              to: "/progress",
              label: "Progress",
              icon: ChartNoAxesColumnIncreasing,
            },
          ].map(({ to, label, icon: Icon }) => (
            <NavLink end={to === "/"} key={to} to={to}>
              <Icon size={21} />
              <span>{label}</span>
            </NavLink>
          ))}
        </nav>
        <OvertakeOverlay />
      </div>
    </RunProvider>
  );
}
function AppRoutes() {
  const { pathname } = useLocation();
  useLayoutEffect(() => {
    window.scrollTo(0, 0);
  }, [pathname]);
  const [identity, setIdentity] = useState(readIdentity);
  const navigate = useNavigate();
  const login = (value: LoginResponse) => {
    saveIdentity(value);
    setIdentity(value);
    navigate("/", { replace: true });
  };
  return (
    <Routes>
      <Route
        path="/login"
        element={
          identity ? <Navigate to="/" replace /> : <LoginPage onLogin={login} />
        }
      />
      <Route
        element={
          identity ? (
            <Shell identity={identity} />
          ) : (
            <Navigate to="/login" replace />
          )
        }
      >
        <Route index element={<HomePage />} />
        <Route path="ranking" element={<RankingPage />} />
        <Route path="map" element={<ActivityMapPage />} />
        <Route path="progress" element={<ProgressPage />} />
      </Route>
      <Route path="*" element={<Navigate to="/" replace />} />
    </Routes>
  );
}
export default function App() {
  return (
    <QueryClientProvider client={client}>
      <MotionConfig reducedMotion="user">
        <BrowserRouter>
          <AppRoutes />
        </BrowserRouter>
      </MotionConfig>
    </QueryClientProvider>
  );
}
