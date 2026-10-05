import { RouterProvider } from "react-router-dom";
import { router } from "./router";

/**
 * App.tsx — Điểm vào chính của ứng dụng Frontend.
 * Đơn giản hóa vai trò: Chỉ cung cấp RouterProvider cho toàn bộ hệ thống routes.
 */
function App() {
  return <RouterProvider router={router} />;
}

export default App;
