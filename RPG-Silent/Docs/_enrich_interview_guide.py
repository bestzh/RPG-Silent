# -*- coding: utf-8 -*-
"""Enrich interview guide with detailed, readable answers."""
import re
import json
from pathlib import Path

# Load enrichments from companion JSON (generated alongside)
ENRICH_PATH = Path(__file__).parent / "interview_enrichments.json"


def parse_cards(content: str) -> list[dict]:
    rows = []
    pattern = r'<a id="q(\d+)"></a>\n\n### Q\d{3} (.+?)\n\n\| 维度 \| 内容 \|\n\|[-| ]+\|\n((?:\| .+\|\n)+)\n---'
    for m in re.finditer(pattern, content, re.S):
        num = int(m.group(1))
        question = m.group(2).strip()
        table = m.group(3)
        fields = {}
        for row in re.finditer(r'\| \*\*(.+?)\*\* \| (.+?) \|', table):
            fields[row.group(1)] = row.group(2).strip()
        is_star = "答题框架" in fields
        rows.append({
            "num": num,
            "question": question,
            "is_star": is_star,
            "points": fields.get("考察点", ""),
            "answer": fields.get("标准答案") or fields.get("答题框架", ""),
            "principle": fields.get("原理解析") or fields.get("表达要点", ""),
            "project": fields.get("项目实战") or fields.get("可举项目", ""),
            "mistakes": fields.get("常见错误") or fields.get("常见扣分", ""),
            "followup": fields.get("面试追问", ""),
            "summary": fields.get("一句话总结", ""),
        })
    return rows


def default_enrich(row: dict) -> dict:
    """Fallback: turn terse fields into structured bullets without duplicating."""
    a = row["answer"].strip()
    p = row["principle"].strip()
    proj = row["project"].strip()
    err = row["mistakes"].strip()
    if p.rstrip("。") in a:
        p = ""
    return {
        "answer": a,
        "principle": p or "见上方面试回答。",
        "project": proj,
        "mistakes": err,
    }


def render_card(row: dict, e: dict) -> str:
    n = row["num"]
    if row["is_star"]:
        return (
            f'<a id="q{n:03d}"></a>\n\n'
            f'### Q{n:03d} {row["question"]}\n\n'
            f'**考察点**：{row["points"]}\n\n'
            f'**答题框架（建议 STAR）**\n\n{e["answer"]}\n\n'
            f'**表达要点**\n\n{e["principle"]}\n\n'
            f'**可举的项目例子**\n\n{e["project"]}\n\n'
            f'**常见扣分点**\n\n{e["mistakes"]}\n\n'
            f'**追问准备**：{row["followup"]}\n\n'
            f'**一句话总结**：{row["summary"]}\n\n'
            f'---\n'
        )
    return (
        f'<a id="q{n:03d}"></a>\n\n'
        f'### Q{n:03d} {row["question"]}\n\n'
        f'**考察点**：{row["points"]}\n\n'
        f'**面试回答（这样答）**\n\n{e["answer"]}\n\n'
        f'**原理说明**\n\n{e["principle"]}\n\n'
        f'**Unity 项目举例**\n\n{e["project"]}\n\n'
        f'**常见踩坑**\n\n{e["mistakes"]}\n\n'
        f'**追问准备**：{row["followup"]}\n\n'
        f'**一句话总结**：{row["summary"]}\n\n'
        f'---\n'
    )


def main():
    md_path = Path(__file__).parent / "Unity中高级开发面试宝典_2026版.md"
    content = md_path.read_text(encoding="utf-8")
    enrichments = json.loads(ENRICH_PATH.read_text(encoding="utf-8"))

    rows = parse_cards(content)
    card_map = {r["num"]: r for r in rows}

    parts = re.split(r'(?=## 第\d+章)', content)
    header = parts[0]
    out = [header.rstrip(), ""]

    for part in parts[1:]:
        if "完整版题量校验" in part:
            out.append(part.lstrip())
            continue
        intro_end = part.find('<a id="q')
        if intro_end == -1:
            out.append(part)
            continue
        out.append(part[:intro_end].rstrip())
        out.append("")
        nums = [int(x) for x in re.findall(r'<a id="q(\d+)"></a>', part[intro_end:])]
        for num in nums:
            row = card_map[num]
            key = str(num)
            e = enrichments.get(key) or default_enrich(row)
            out.append(render_card(row, e))
        out.append("")

    result = "\n".join(out)
    result = re.sub(r'\n{4,}', '\n\n\n', result)
    md_path.write_text(result, encoding="utf-8")
    covered = sum(1 for n in card_map if str(n) in enrichments)
    print(f"Rendered {len(rows)} cards, {covered} with detailed enrichments.")


if __name__ == "__main__":
    main()
