# 无效夹具范围

`raw.json` 保留首次320场历史，但其中30场无效：`lab-shield-chain` 与 `lab-crit-attack` 在末区普通、精英、Boss各5种子时，第4名近战覆盖既有部署格，实际只准备4名英雄。

这30场不得用于强度判断。`raw-corrected.json` 是相同键的修正复测；`final.json` 用它逐键替换无效行，形成320场有效主矩阵。`raw-followup.json` 是另行登记的40场追测，不混入主矩阵。
