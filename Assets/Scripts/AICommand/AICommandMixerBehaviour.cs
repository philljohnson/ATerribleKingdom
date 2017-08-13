using System;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

public class AICommandMixerBehaviour : PlayableBehaviour
{
	private Unit m_TrackBinding;
	private bool m_FirstFrameHappened = false;
	private Vector3 m_DefaultPosition, previousInputFinalPosition;
	private int m_lastInputPlayed = -1;

    public override void ProcessFrame(Playable playable, FrameData info, object playerData)
    {
		m_TrackBinding = playerData as Unit;

        if (!m_TrackBinding)
			return;

		if (!m_FirstFrameHappened)
		{
			m_DefaultPosition = m_TrackBinding.transform.position;
			m_FirstFrameHappened = true;
		}

		if(Application.isPlaying)
		{
			ProcessPlayModeFrame(playable);
		}
		else
		{
			ProcessEditModeFrame(playable);
		}
    }

	private void ProcessEditModeFrame(Playable playable)
	{
		previousInputFinalPosition = m_DefaultPosition;
		int inputCount = playable.GetInputCount();

		for (int i = 0; i < inputCount; i++)
		{
			float inputWeight = playable.GetInputWeight(i);
			ScriptPlayable<AICommandBehaviour> inputPlayable = (ScriptPlayable<AICommandBehaviour>)playable.GetInput(i);
			AICommandBehaviour input = inputPlayable.GetBehaviour();

			//force the finalPosition to the attack target in case of an Attack action
			if(input.actionType == AICommand.CommandType.AttackTarget)
			{
				input.targetPosition = input.targetUnit.transform.position;
			}

			if(inputWeight > 0f)
			{
				double progress = inputPlayable.GetTime()/inputPlayable.GetDuration();
				m_TrackBinding.transform.position = Vector3.Lerp(previousInputFinalPosition, input.targetPosition, (float)progress);

				continue;
			}
			else
			{
				previousInputFinalPosition = input.targetPosition; //cached to act as initial position for the next input
			}
		}
	}

	private void ProcessPlayModeFrame(Playable playable)
	{
		int inputCount = playable.GetInputCount();

		for(int i = 0; i < inputCount; i++)
		{
			float inputWeight = playable.GetInputWeight(i);
			ScriptPlayable<AICommandBehaviour> inputPlayable = (ScriptPlayable<AICommandBehaviour>)playable.GetInput(i);
			AICommandBehaviour input = inputPlayable.GetBehaviour();


			//Make the Unit script execute the command
			if(inputWeight > 0f)
			{
				if(m_lastInputPlayed != i
				   && !input.commandExecuted)
				{
					AICommand c = new AICommand(input.actionType, input.targetPosition, input.targetUnit);
					m_TrackBinding.ExecuteCommand(c);
					input.commandExecuted = true;
				}
			}
		}
	}

	public override void OnGraphStop (Playable playable)
	{
		if(!Application.isPlaying)
		{
			m_FirstFrameHappened = false;
			
			if (m_TrackBinding == null)
				return;
			
			m_TrackBinding.transform.position = m_DefaultPosition;
		}
	}
}
