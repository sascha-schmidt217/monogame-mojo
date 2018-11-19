// Mojo.Input.h

#pragma once
#define WIN32_LEAN_AND_MEAN

#include <windows.h>
#include <mmsystem.h>

using namespace System;

namespace WindowsInput {

	public enum class DataCommands
	{
		JOYX, JOYY, JOYZ, JOYR, JOYU, JOYV, JOYYAW, JOYPITCH, JOYROLL, JOYHAT, JOYWHEEL
	};

	public ref class GamePad
	{	
		array<int>^ joyhandle;
		array<long long>^ joy_time;// [16]
		array<int>^ joy_buttons;//[16]
		array<float>^ joy_axis;//[16 * 16]
		array<int>^ joy_hits;//[16, 16]

	public:

		GamePad()
		{
			joyhandle = gcnew array<int>(256);
			joy_time = gcnew array<long long>(16);
			joy_buttons = gcnew array<int>(16);
			joy_axis = gcnew array<float>(16*16);
			joy_hits = gcnew array<int>(16*16);
			JoyCount();//'required to kick starts some drivers
		}

		System::String^ JoyName(int port)
		{
			return gcnew System::String(JoyCName(port));
		}

		int JoyCount()
		{
			JOYINFO		j;
			int			n, i, t, res;

			n = joyGetNumDevs();
			t = 0;
			for (i = 0; i<n; i++)
			{
				res = joyGetPos(i, &j);
				if (res == JOYERR_NOERROR && t<256) joyhandle[t++] = i;
			}
			return t;
		}

		bool JoyHit(int button, int port)
		{
			SampeJoy(port);
			int n = joy_hits[port * 16 + button];
			joy_hits[port * 16 + button] = 0;
			return n != 0;
		}

		bool JoyDown(int button, int port)
		{
			SampeJoy(port);
			return (joy_buttons[port] & (1 << (int)button)) != 0;
		}

		float JoyX(int port)
		{
			SampeJoy(port);
			return joy_axis[port*16 + (int)DataCommands::JOYX];
		}

		float JoyY(int port)
		{
			SampeJoy(port);
			return joy_axis[port * 16 + (int)DataCommands::JOYY];
		}

	private:

		void SampeJoy(int port)
		{

			auto time = DateTime::Now.Ticks / TimeSpan::TicksPerMillisecond;
			auto t = joy_time[port] - time;
			if (t < 0 || t > 0)
			{
				int old = joy_buttons[port];
				int btn = 0;
				float axis[16];
				ReadJoy(port, &btn, axis);

				joy_buttons[port] = btn;
				for(int i = 0; i < 16;++i)
					joy_axis[port * 16 + i] = axis[i];

				for (int button = 0; button < 16; ++button)
				{
					int b = 1 << button;
					if (!(old & b) && joy_buttons[port] & b)
						joy_hits[button + port*16]++;//button and port were t'other way round.

				}
			}
		}

		WCHAR *JoyCName(int port)
		{
			static JOYCAPS joycaps;
			int		res;

			port = joyhandle[port];
			res = joyGetDevCaps(port, &joycaps, sizeof(JOYCAPS));
			if (res != JOYERR_NOERROR) return 0;
			return joycaps.szPname;
		}

		int JoyButtonCaps(int port)
		{
			JOYCAPS	caps;
			int		res, mask;

			port = joyhandle[port];
			res = joyGetDevCaps(port, &caps, sizeof(JOYCAPS));
			if (res != JOYERR_NOERROR) return 0;
			mask = (1 << caps.wNumButtons) - 1;
			return mask;
		}

		int JoyAxisCaps(int port)
		{
			JOYCAPS	caps;
			int		res, mask;

			port = joyhandle[port];
			res = joyGetDevCaps(port, &caps, sizeof(JOYCAPS));
			if (res != JOYERR_NOERROR) return 0;
			mask = (1 << caps.wNumAxes) - 1;
			if (caps.wCaps&JOYCAPS_HASPOV) mask |= (1 << (int)DataCommands::JOYHAT);
			return mask;
		}

		int ReadJoy(int port, int *buttons, float *axis)
		{
			JOYCAPS		caps;
			JOYINFOEX	j;
			int			res, f, pov;

			port = joyhandle[port];
			res = joyGetDevCaps(port, &caps, sizeof(JOYCAPS));
			if (res != JOYERR_NOERROR) return 0;
			j.dwSize = sizeof(JOYINFOEX);
			j.dwFlags = JOY_RETURNALL;
			res = joyGetPosEx(port, &j);
			if (res != JOYERR_NOERROR) return 0;
			*buttons = j.dwButtons;
			f = j.dwFlags;
			if (f&JOY_RETURNX) axis[(int)DataCommands::JOYX] = -1.0 + 2.0*(j.dwXpos - caps.wXmin) / caps.wXmax;
			if (f&JOY_RETURNY) axis[(int)DataCommands::JOYY] = -1.0 + 2.0*(j.dwYpos - caps.wYmin) / caps.wYmax;
			if (f&JOY_RETURNZ) axis[(int)DataCommands::JOYZ] = -1.0 + 2.0*(j.dwZpos - caps.wZmin) / caps.wZmax;
			if (f&JOY_RETURNR) axis[(int)DataCommands::JOYR] = -1.0 + 2.0*(j.dwRpos - caps.wRmin) / caps.wRmax;
			if (f&JOY_RETURNU) axis[(int)DataCommands::JOYU] = -1.0 + 2.0*(j.dwUpos - caps.wUmin) / caps.wUmax;
			if (f&JOY_RETURNV) axis[(int)DataCommands::JOYV] = -1.0 + 2.0*(j.dwVpos - caps.wVmin) / caps.wVmax;
			if (f&JOY_RETURNPOV)
			{
				pov = j.dwPOV;
				if (pov<0 || pov>36000) axis[(int)DataCommands::JOYHAT] = -1.0; else axis[(int)DataCommands::JOYHAT] = pov / 36000.0;
			}
			return 1;
		}

		void WriteJoy(int port, int channel, float value)
		{
			port = joyhandle[port];
		}
	};
}
