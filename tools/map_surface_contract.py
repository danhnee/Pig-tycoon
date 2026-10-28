"""Canonical map surface/UV contract, extracted without numerical changes."""
import math
import numpy as np

WORLD_X_MAX = 31.0

WORLD_Y_MAX = 23.5

ISLAND_RX = 29.2

ISLAND_RY = 21.4

def strip_uv(strip_idx, u_loc, v_loc):
    """
    Maps local [0..1] UVs of a 3D module into Strip `strip_idx` (0..7) in the bottom 25% of the 4K atlas.
    `v_out` in [0.006, 0.244] corresponds to rows [4071..3096] in image space.
    """
    u_s = (strip_idx + 0.03 + 0.94 * np.clip(u_loc, 0.0, 1.0)) / 8.0
    v_s = 0.006 + 0.238 * np.clip(v_loc, 0.0, 1.0)
    return np.stack([u_s, v_s], axis=-1).astype(np.float32)

def eval_river_center_and_width(yw):
    y_arr = np.asarray(yw, dtype=np.float32)
    t = np.clip((12.0 - y_arr) / 18.6, 0.0, 1.0)
    cx = 13.4 + 1.3 * np.sin(t * 1.50) + 13.6 * (t ** 2.05)
    hw = 5.0 - 0.55 * np.sin(t * 2.4) + 1.65 * (t ** 1.5)
    return cx, hw

def eval_road_mask(xw, yw):
    x = np.asarray(xw, dtype=np.float32)
    y = np.asarray(yw, dtype=np.float32)

    y_p1 = 5.0 + 0.75 * np.sin((x + 17.0) * 0.21) + 0.045 * (x + 12.0)
    d1 = np.abs(y - y_p1)
    m1 = np.where((x >= -20.5) & (x <= 11.5), np.clip(1.0 - d1 / 1.15, 0.0, 1.0), 0.0)

    t2 = np.clip((x + 15.0) / 31.0, 0.0, 1.0)
    y_p2 = 4.5 * (1.0 - t2) + (-9.8) * t2 + 1.05 * np.sin(t2 * math.pi * 2.0)
    d2 = np.abs(y - y_p2) * 0.90
    m2 = np.where((x >= -15.5) & (x <= 16.5), np.clip(1.0 - d2 / 1.05, 0.0, 1.0), 0.0)

    c_farm = np.clip(1.0 - np.sqrt(((x + 16.2) / 4.2) ** 2 + ((y - 6.8) / 2.8) ** 2), 0.0, 1.0)
    c_obs = np.clip(1.0 - np.sqrt(((x + 7.6) / 3.5) ** 2 + ((y - 5.8) / 2.4) ** 2), 0.0, 1.0)

    mask = np.maximum.reduce([m1 * 0.85, m2 * 0.72, c_farm * 0.80, c_obs * 0.80])
    return np.clip(mask, 0.0, 1.0)

def eval_pond_distances(xw, yw):
    x = np.asarray(xw, dtype=np.float32)
    y = np.asarray(yw, dtype=np.float32)
    lx1 = (x + 3.4) * 0.8572 - (y + 5.2) * 0.5150
    ly1 = (x + 3.4) * 0.5150 + (y + 5.2) * 0.8572
    p1_d = np.sqrt((lx1 / 5.65) ** 2 + (ly1 / 1.78) ** 2)

    lx2 = (x + 0.5) * 0.9511 + (y - 0.6) * 0.3090
    ly2 = -(x + 0.5) * 0.3090 + (y - 0.6) * 0.9511
    p2_d = np.sqrt((lx2 / 3.55) ** 2 + (ly2 / 1.40) ** 2)

    lx3 = (x - 9.6) * 0.9455 - (y + 7.4) * 0.3256
    ly3 = (x - 9.6) * 0.3256 + (y + 7.4) * 0.9455
    p3_d = np.sqrt((lx3 / 3.35) ** 2 + (ly3 / 1.38) ** 2)
    return p1_d, p2_d, p3_d

def sample_ground_z(xw, yw):
    x = np.asarray(xw, dtype=np.float32)
    y = np.asarray(yw, dtype=np.float32)

    z = (
        2.32
        + 1.45 * np.clip((y + 16.0) / 34.0, 0.0, 1.0)
        + 1.15 * np.clip((-x + 10.0) / 38.0, 0.0, 1.0) * np.clip((y + 5.0) / 24.0, 0.0, 1.0)
        + 0.22 * np.sin(x * 0.21 + y * 0.17) * np.cos(x * 0.14 - y * 0.22)
    )

    castle_plat = np.clip((x - 4.0) / 6.0, 0.0, 1.0) * np.clip((y - 11.2) / 3.0, 0.0, 1.0)
    z = z + 5.40 * (castle_plat ** 1.25)

    alpine_rise = np.clip((y - 11.2) / 6.5, 0.0, 1.0) * np.clip((7.5 - x) / 12.0, 0.0, 1.0)
    z = z + 3.50 * (alpine_rise ** 1.35)

    p1_d, p2_d, p3_d = eval_pond_distances(x, y)
    w1 = np.clip((1.16 - p1_d) / 0.14, 0.0, 1.0)
    w2 = np.clip((1.16 - p2_d) / 0.14, 0.0, 1.0)
    w3 = np.clip((1.16 - p3_d) / 0.14, 0.0, 1.0)
    z = z * (1.0 - w1) + (2.34 - 0.68) * w1
    z = z * (1.0 - w2) + (2.56 - 0.68) * w2
    z = z * (1.0 - w3) + (2.14 - 0.68) * w3

    riv_cx, riv_hw = eval_river_center_and_width(y)
    in_riv_y = np.clip((12.5 - y) / 0.7, 0.0, 1.0) * np.clip((y + 8.4) / 0.7, 0.0, 1.0)
    riv_norm = np.abs(x - riv_cx) / np.maximum(riv_hw, 1.0)
    riv_carve = np.clip((1.16 - riv_norm) / 0.20, 0.0, 1.0) * in_riv_y
    z = z * (1.0 - riv_carve) + 0.65 * riv_carve

    return z

def island_signed_distance(xw, yw):
    x = np.asarray(xw, dtype=np.float32)
    y = np.asarray(yw, dtype=np.float32)
    ang = np.arctan2(y, x)
    wob = (
        0.032 * np.sin(ang * 3.0 + 0.4)
        + 0.022 * np.cos(ang * 5.0 - 0.7)
        + 0.012 * np.sin(ang * 9.0 + 1.2)
    )
    rx = ISLAND_RX * (1.0 + wob)
    ry = ISLAND_RY * (1.0 + wob)
    d = (np.abs(x / rx) ** 2.35 + np.abs(y / ry) ** 2.35) ** (1.0 / 2.35)
    return d
