import type { Metadata } from "next";
import ModelLab from "./ModelLab";
import "./model-lab.css";
import "./decision-scene.css";

export const metadata: Metadata = {
  title: "游戏基础规则网络 · 塔军玩法模型实验室",
  description: "通过可编辑关系图检验基础规则、传播影响、内容接入和调研证据。",
};

export default function ModelLabPage() {
  return <ModelLab />;
}
