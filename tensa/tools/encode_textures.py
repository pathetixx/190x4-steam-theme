#!/usr/bin/env python3
"""Перекодирует фоновые текстуры без блоков: тёмные плавные области webp сжимает слишком грубо,
поэтому качество высокое, а лёгкое зерно убирает ступеньки на градиентах.
Запуск: encode_textures.py <папка генерации> <assets Tensa> <assets темы>"""
import sys
import numpy as np
from PIL import Image

gen, tensa, out = sys.argv[1:4]

def grain(im, amount=1.6, seed=7):
    a = np.asarray(im.convert("RGB")).astype(np.float32)
    n = np.random.default_rng(seed).normal(0, amount, a.shape[:2])[..., None]
    return Image.fromarray(np.clip(a + n, 0, 255).astype(np.uint8))

def save_rgb(im, name, q=90):
    im.save(f"{out}/{name}", "WEBP", quality=q, method=6, use_sharp_yuv=True)

def save_rgba(path, name, width, q=90):
    im = Image.open(path).convert("RGBA")
    if im.width > width:
        im = im.resize((width, round(im.height * width / im.width)), Image.LANCZOS)
    im.save(f"{out}/{name}", "WEBP", quality=q, alpha_quality=100, method=6, use_sharp_yuv=True)

paper = Image.open(f"{gen}/w-paper2/paper.png").convert("RGB")
print("paper source", paper.size)
save_rgb(grain(paper.resize((2048, round(paper.height * 2048 / paper.width)), Image.LANCZOS)), "paper.webp")
store = Image.open(f"{gen}/w-storebg/storebg.png").convert("RGB")
save_rgb(grain(store.resize((2048, round(store.height * 2048 / store.width)), Image.LANCZOS)), "storebg.webp")
btn = Image.open(f"{gen}/w-btn/btn.png").convert("RGB")
save_rgb(btn.resize((840, round(btn.height * 840 / btn.width)), Image.LANCZOS), "btn.webp")
save_rgba(f"{gen}/w-splash/splash.png", "splash.webp", 1400)
save_rgba(f"{tensa}/brush.png", "brush.webp", 640)
