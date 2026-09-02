import type { Metadata } from "next";
import "./globals.css";

export const metadata: Metadata = {
  title: "塔军机制设计图谱",
  description: "塔军自走构筑游戏的机制维度、构筑引擎与 Boss 试卷探索工作台。",
  robots: {
    index: false,
    follow: false,
    nocache: true,
    googleBot: {
      index: false,
      follow: false,
      noimageindex: true,
    },
  },
};

export default function RootLayout({
  children,
}: Readonly<{
  children: React.ReactNode;
}>) {
  return (
    <html lang="zh-CN"><body>{children}</body></html>
  );
}
