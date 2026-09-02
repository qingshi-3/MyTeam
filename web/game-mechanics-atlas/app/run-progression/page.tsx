import type { Metadata } from "next";
import RunProgressionBrowser from "./RunProgressionBrowser";
import "./run-progression.css";

export const metadata: Metadata = {
  title: "十种局内进程机制｜塔军机制设计图谱",
  description: "十套服务构筑、发育、自动战斗与肉鸽随机性的完整局内进程机制。",
};

export default function RunProgressionPage() {
  return <RunProgressionBrowser />;
}
