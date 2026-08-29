using System;
using System.Collections.Generic;
using System.Text;

namespace DualSenser.Service.Models;

public readonly record struct TouchPoint(bool IsTouching, int TouchId, int X, int Y);

public sealed record DualSenseInputState(
    byte LeftStickX,
    byte LeftStickY,
    byte RightStickX,
    byte RightStickY,
    byte L2Trigger,
    byte R2Trigger,
    bool Square,
    bool Cross,
    bool Circle,
    bool Triangle,
    bool DPadUp,
    bool DPadDown,
    bool DPadLeft,
    bool DPadRight,
    bool L1,
    bool R1,
    bool L2Button,
    bool R2Button,
    bool Create,
    bool Options,
    bool L3,
    bool R3,
    bool PSButton,
    bool TouchpadClick,
    bool MicMute,
    TouchPoint Touch1,
    TouchPoint Touch2
)
{
    public static DualSenseInputState Empty => new(
        LeftStickX: 128,
        LeftStickY: 128,
        RightStickX: 128,
        RightStickY: 128,
        L2Trigger: 0,
        R2Trigger: 0,
        Square: false,
        Cross: false,
        Circle: false,
        Triangle: false,
        DPadUp: false,
        DPadDown: false,
        DPadLeft: false,
        DPadRight: false,
        L1: false,
        R1: false,
        L2Button: false,
        R2Button: false,
        Create: false,
        Options: false,
        L3: false,
        R3: false,
        PSButton: false,
        TouchpadClick: false,
        MicMute: false,
        Touch1: new TouchPoint(false, 0, 0, 0),
        Touch2: new TouchPoint(false, 0, 0, 0)
    );

    public List<string> GetActivityDifferences(DualSenseInputState previous)
    {
        var diffs = new List<string>();

        // 1. Botões de Ação
        CheckButton(diffs, "Quadrado", Square, previous.Square);
        CheckButton(diffs, "Cruz (X)", Cross, previous.Cross);
        CheckButton(diffs, "Círculo (O)", Circle, previous.Circle);
        CheckButton(diffs, "Triângulo", Triangle, previous.Triangle);

        // 2. D-Pad
        CheckButton(diffs, "D-Pad Cima", DPadUp, previous.DPadUp);
        CheckButton(diffs, "D-Pad Baixo", DPadDown, previous.DPadDown);
        CheckButton(diffs, "D-Pad Esquerda", DPadLeft, previous.DPadLeft);
        CheckButton(diffs, "D-Pad Direita", DPadRight, previous.DPadRight);

        // 3. Botões de Ombro e Gatilhos Digitais
        CheckButton(diffs, "L1", L1, previous.L1);
        CheckButton(diffs, "R1", R1, previous.R1);
        CheckButton(diffs, "L2 (Clique)", L2Button, previous.L2Button);
        CheckButton(diffs, "R2 (Clique)", R2Button, previous.R2Button);

        // 4. Analógicos em Clique (L3 / R3)
        CheckButton(diffs, "L3 (Analógico Esq.)", L3, previous.L3);
        CheckButton(diffs, "R3 (Analógico Dir.)", R3, previous.R3);

        // 5. Botões Especiais
        CheckButton(diffs, "Create/Share", Create, previous.Create);
        CheckButton(diffs, "Options", Options, previous.Options);
        CheckButton(diffs, "PS Button", PSButton, previous.PSButton);
        CheckButton(diffs, "Touchpad (Clique)", TouchpadClick, previous.TouchpadClick);
        CheckButton(diffs, "Mute", MicMute, previous.MicMute);

        // 6. Gatilhos Analógicos (L2 e R2) - detecta variação significativa (>= 20 unidades) ou pressionamento
        if (Math.Abs(L2Trigger - previous.L2Trigger) >= 20 || (L2Trigger > 0 && previous.L2Trigger == 0) || (L2Trigger == 0 && previous.L2Trigger > 0))
        {
            int percent = (L2Trigger * 100) / 255;
            diffs.Add($"Gatilho L2: {percent}% ({L2Trigger}/255)");
        }

        if (Math.Abs(R2Trigger - previous.R2Trigger) >= 20 || (R2Trigger > 0 && previous.R2Trigger == 0) || (R2Trigger == 0 && previous.R2Trigger > 0))
        {
            int percent = (R2Trigger * 100) / 255;
            diffs.Add($"Gatilho R2: {percent}% ({R2Trigger}/255)");
        }

        // 7. Analógico Esquerdo (LX, LY) - variação significativa fora da deadzone central
        if (HasStickMoved(LeftStickX, LeftStickY, previous.LeftStickX, previous.LeftStickY))
        {
            diffs.Add($"Analógico Esquerdo: X={LeftStickX}, Y={LeftStickY}");
        }

        // 8. Analógico Direito (RX, RY)
        if (HasStickMoved(RightStickX, RightStickY, previous.RightStickX, previous.RightStickY))
        {
            diffs.Add($"Analógico Direito: X={RightStickX}, Y={RightStickY}");
        }

        // 9. Trackpad (Touch 1 e Touch 2)
        if (Touch1.IsTouching != previous.Touch1.IsTouching)
        {
            if (Touch1.IsTouching)
                diffs.Add($"Trackpad: Dedo encostado (X={Touch1.X}, Y={Touch1.Y})");
            else
                diffs.Add("Trackpad: Dedo retirado");
        }
        else if (Touch1.IsTouching && (Math.Abs(Touch1.X - previous.Touch1.X) >= 40 || Math.Abs(Touch1.Y - previous.Touch1.Y) >= 40))
        {
            diffs.Add($"Trackpad Deslizando: X={Touch1.X}, Y={Touch1.Y}");
        }

        if (Touch2.IsTouching != previous.Touch2.IsTouching)
        {
            if (Touch2.IsTouching)
                diffs.Add($"Trackpad: Segundo dedo encostado (X={Touch2.X}, Y={Touch2.Y})");
            else
                diffs.Add("Trackpad: Segundo dedo retirado");
        }
        else if (Touch2.IsTouching && (Math.Abs(Touch2.X - previous.Touch2.X) >= 40 || Math.Abs(Touch2.Y - previous.Touch2.Y) >= 40))
        {
            diffs.Add($"Trackpad Segundo Dedo Deslizando: X={Touch2.X}, Y={Touch2.Y}");
        }

        return diffs;
    }

    private static void CheckButton(List<string> diffs, string buttonName, bool current, bool previous)
    {
        if (current && !previous)
        {
            diffs.Add($"Botão Pressionado: [{buttonName}]");
        }
        else if (!current && previous)
        {
            diffs.Add($"Botão Solto: [{buttonName}]");
        }
    }

    private static bool HasStickMoved(byte curX, byte curY, byte prevX, byte prevY)
    {
        const int threshold = 25;
        const int centerMin = 128 - 20;
        const int centerMax = 128 + 20;

        bool curInCenter = curX >= centerMin && curX <= centerMax && curY >= centerMin && curY <= centerMax;
        bool prevInCenter = prevX >= centerMin && prevX <= centerMax && prevY >= centerMin && prevY <= centerMax;

        // Se voltou para o centro
        if (curInCenter && !prevInCenter)
            return true;

        // Se saiu do centro ou moveu significativamente
        if (!curInCenter && (Math.Abs(curX - prevX) >= threshold || Math.Abs(curY - prevY) >= threshold))
            return true;

        return false;
    }
}
