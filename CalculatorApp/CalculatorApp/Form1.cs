using System;
using System.Drawing;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace CalculatorApp
{
	public partial class 電卓 : Form
	{
		/// <summary>
		/// アプリ全体で使用する定数
		/// </summary>
		internal static class Constants
		{
			/// <summary>
			/// フォントサイズ関連の定数
			/// </summary>
			internal static class FontSize
			{
				/// <summary>
				/// エラー表示時のフォントサイズ
				/// </summary>
				internal const float ERROR_MESSAGE = 18.0f;

				/// <summary>
				/// 計算結果表示欄の基準フォントサイズ
				/// </summary>
				internal const float RESULT_DISPLAY_BASE = 36f;

				/// <summary>
				/// 途中計算表示欄の基準フォントサイズ
				/// </summary>
				internal const float EXPRESSION_DISPLAY_BASE = 10f;

				/// <summary>
				/// 計算結果表示欄の最小フォントサイズ
				/// </summary>
				internal const float MIN_LIMIT = 14f;

				/// <summary>
				/// フォント縮小時のステップ幅
				/// </summary>
				internal const float REDUCTION_STEP = 0.5f;

				/// <summary>
				/// フォントの再設定を判定するためのしきい値
				/// </summary>
				internal const float SIZE_EPSILON = 0.1f;
			}

			/// <summary>
			/// 画面表示に使う演算子
			/// </summary>
			internal static class Symbol
			{
				/// <summary>
				/// 加算記号
				/// </summary>
				internal const string ADD = "+";

				/// <summary>
				/// 減算記号
				/// </summary>
				internal const string SUBTRACT = "-";

				/// <summary>
				///乗算記号
				/// </summary>
				internal const string MULTIPLY = "×";

				/// <summary>
				/// 除算記号
				/// </summary>
				internal const string DIVIDE = "÷";

				/// <summary>
				/// イコール記号
				/// </summary>
				internal const string EQUAL = "=";
			}

			/// <summary>
			/// 数値関連の定数
			/// </summary>
			internal static class Numeric
			{
				/// <summary>
				/// 初期値
				/// </summary>
				internal const decimal INITIAL_VALUE = 0m;

				/// <summary>
				/// 表示用のゼロ値文字列。
				/// </summary>
				public const string ZERO_VALUE = "0";

				/// <summary>
				/// パーセント計算用の乗数 (1/100)。
				/// </summary>
				internal const decimal PERCENT_MULTIPLY = 0.01m;

				/// <summary>
				/// 通常表示で許可する最大桁数
				/// </summary>
				internal const int MAX_DISPLAY_DIGITS = 16;

				/// <summary>
				/// 0.形式の小数の時の最大桁数
				/// </summary>
				internal const int MAX_FRACTIONAL_DIGITS = 17;

				/// <summary>
				// 有効桁数・指数切替
				/// </summary>
				public const int MAX_SIGNIFICANT_DIGITS = 17;

				/// <summary>
				/// 指数に切り替えるためのしきい値
				/// </summary>
				internal static readonly decimal SCI_SMALL_THRESHOLD = 1e-3m;

				/// <summary>
				/// 指数に切り替えるためのしきい値
				/// </summary>
				internal static readonly decimal SCI_LARGE_THRESHOLD = 1e16m;

				/// <summary>
				/// 表示用に保持する有効桁数
				/// </summary>
				public const int EXP_SIGNIFICANT_DIGITS = 16; 
			}

			/// <summary>
			/// エラー表示用のメッセージ
			/// </summary>
			internal static class ErrorMessage
			{	
				/// <summary>
				/// 計算可能な範囲を超えた場合のエラーメッセージ
				/// </summary>
				internal const string OVERFLOW = "計算範囲を超えました";

				/// <summary>
				/// 0除算時のエラーメッセージ
				/// </summary>
				internal const string DIVIDE_BY_ZERO = "0で割ることはできません";

				/// <summary>
				/// 0÷0を行った場合のエラーメッセージ
				/// </summary>
				internal const string UNDEFINED = "結果が定義されていません";
			}

			/// <summary>
			/// 特殊表示
			/// </summary>
			internal static class SpecialDisplay
			{
				/// <summary>
				/// サインチェンジキーを押したときに表示される定数
				/// </summary>
				internal const string NEGATE_FUNCTION = "negate";
			}
		}

		/// <summary>
		/// 現在選択されている演算子の種類を定義する列挙型
		/// </summary>
		private enum OperatorType
		{
			/// <summary>演算なし</summary>
			NON,
			/// <summary>加算</summary>
			ADD,
			/// <summary>減算</summary>
			SUBTRACT,
			/// <summary>乗算</summary>
			MULTIPLY,
			/// <summary>除算</summary>
			DIVIDE
		}

		/// <summary>計算処理の結果を表すコード</summary>
		private enum ErrorCode
		{
			/// <summary>成功 </summary>
			Success,
			/// <summary> 0÷0結果が定義されていない </summary>
			Undefined,      
			/// <summary> 0除算</summary>
			DivideByZero    
		}

		//内部状態を管理する変数

		/// <summary>
		/// 最初の値
		/// </summary>
		private decimal m_firstValue = 0m;

		/// <summary>
		/// 2番目の値
		/// </summary>
		private decimal m_secondValue = 0m;

		/// <summary>
		/// 上書き入力フラグ
		/// </summary>
		private bool m_textOverwrite = false;

		/// <summary>
		/// 小数点入力済みフラグ
		/// summary>
		private bool m_numDot = false;

		/// <summary>
		/// エラー状態
		/// </summary>
		private bool m_isErrorState = false;

		/// <summary>
		/// 現在の入力を CE でクリアしたか
		/// </summary>
		private bool m_isClearEntry = false;

		/// <summary>
		/// 内部の現在表示値（表示文字列と分離）
		/// </summary>
		private decimal m_displayValue = Constants.Numeric.INITIAL_VALUE;

		/// <summary>
		/// ±押下時にフォーマット保持するか
		/// </summary>
		private bool m_preserveFormatOnToggle = false;

		/// <summary>
		/// 直近のユーザー入力
		/// </summary>
		private string m_lastUserTypedRaw = Constants.Numeric.ZERO_VALUE;

		/// <summary>
		/// 直前が％か
		/// </summary>
		private bool m_lastActionWasPercent = false;

		/// <summary>
		/// ＝直後に途中式を消したか
		/// </summary>
		private bool m_clearedExprAfterEqual = false;

		/// <summary>
		/// 基準フォントサイズ（初期値）
		/// </summary>
		private float m_defaultFontSize;

		/// <summary>
		/// 途中式欄の基準フォントサイズ（初期値）
		/// </summary>
		private float m_defaultExpressionFontSize;

		/// <summary>
		/// エラー時に無効化するボタン
		/// </summary>
		private Button[] m_disabledButtonsOnError;

		/// <summary>
		/// % で生成された右辺を編集不可にする
		/// </summary>
		private bool m_lockRhsAfterAutoOp = false;

		/// <summary>
		/// ＝直後%の連打用。 r/100
		/// </summary>
		private decimal m_percentChainFactor = 0m;

		/// <summary>
		/// ＝直後%の連打中か
		/// </summary>
		private bool m_inPercentChainAfterEqual = false;

		/// <summary>
		/// 現在の演算子種別を保持する変数
		/// </summary>
		private OperatorType m_currentOperatorType = OperatorType.NON;

		//ボタンが押された時の処理

		/// <summary>
		/// フォームのコンストラクタ
		/// 表示欄のフォントサイズを設定し、初期値を0にする
		/// </summary>
		public 電卓()
		{
			InitializeComponent();

			this.MaximumSize = this.Size;
			this.MinimumSize = this.Size;
			this.FormBorderStyle = FormBorderStyle.FixedSingle;
			this.MaximizeBox = false;

			//エラー状態で無効かするボタンの一覧
			m_disabledButtonsOnError = new Button[]
            {
                btnDot, btnTogglesign, btnPercent, btnPlus,
                btnMinus, btnMultiply, btnDivide
            };
		}

		/// <summary>
		/// フォームの初期化処理
		/// </summary>
		/// <param name="sender">イベントの発生元</param>
		/// <param name="e">イベントデータ</param>
		private void Form1_Load(object sender, EventArgs e)
		{
			textResult.Text = Constants.Numeric.ZERO_VALUE;
			m_textOverwrite = true;

			textResult.Font = new Font(textResult.Font.FontFamily, Constants.FontSize.RESULT_DISPLAY_BASE, textResult.Font.Style);
			textExpression.Font = new Font(textExpression.Font.FontFamily, Constants.FontSize.EXPRESSION_DISPLAY_BASE, textExpression.Font.Style);

			m_defaultFontSize = textResult.Font.Size;
			m_defaultExpressionFontSize = textExpression.Font.Size;

			m_displayValue = 0m;
			m_lastUserTypedRaw = "0";
		}

		/// <summary>
		/// 結果表示欄のフォントサイズを初期化
		/// </summary>
		/// <param name="sender">イベントの発生元</param>
		/// <param name="e">イベントデータ</param>
		private void textResult_TextChanged(object sender, EventArgs e)
		{
			AutoFitResultFont();
		}

		/// <summary>
		/// 途中式表示欄のテキスト変更イベント
		/// </summary>
		/// <param name="sender">イベントの発生元</param>
		/// <param name="e">イベントデータ</param>
		private void textExpression_TextChanged(object sender, EventArgs e)
		{

		}

		/// <summary>
		/// 数字ボタン押下時のイベントハンドラー
		/// </summary>
		/// <param name="sender">イベントの発生元</param>
		/// <param name="e">イベントデータ</param>
		private void btnNum_Click(object sender, EventArgs e)
		{
			var btn = sender as Button;
			if (btn == null)
			{
				return;
			}
			if (IsError())
			{
				ResetCalculatorState();
			}
			OnDigitButton(btn.Text);
		}

		/// <summary>
		/// 小数点キー入力時の処理
		/// </summary>
		/// <param name="sender">イベントの発生元</param>
		/// <param name="e">イベントデータ</param>
		private void btnDot_Click(object sender, EventArgs e)
		{
			HandleInitialState();
			OnDotButton();
		}

		/// <summary>
		/// 演算子キー入力時の処理
		/// </summary>
		/// <param name="sender">イベントの発生元</param>
		/// <param name="e">イベントデータ</param>
		private void btnOperation_Click(object sender, EventArgs e)
		{
			Button btn = sender as Button;
			if (btn == null)
			{
				return;
			}

			OperatorType op;

			//ボタンの表示テキストに応じて演算子を判定
			switch (btn.Text)
			{
				case Constants.Symbol.ADD:
					op = OperatorType.ADD;
					break;
				case Constants.Symbol.SUBTRACT:
					op = OperatorType.SUBTRACT;
					break;
				case Constants.Symbol.MULTIPLY:
					op = OperatorType.MULTIPLY;
					break;
				case Constants.Symbol.DIVIDE:
					op = OperatorType.DIVIDE;
					break;
				default:
					op = OperatorType.NON;
					break;
			}

			OnOperatorButton(op);
		}

		/// <summary>
		/// イコールキー入力時の処理
		/// </summary>
		/// <param name="sender">イベントの発生元</param>
		/// <param name="e">イベントデータ</param>
		private void btnEnter_Click(object sender, EventArgs e)
		{
			OnEqualsButton();
		}

		/// <summary>
		/// ％キー入力時の処理
		/// </summary>
		/// <param name="sender">イベントの発生元</param>
		/// <param name="e">イベントデータ</param>
		private void btnPercent_Click(object sender, EventArgs e)
		{
			OnPercentButton();
		}

		/// <summary>
		/// クリアエントリーキー入力時の処理
		/// </summary>
		/// <param name="sender">イベントの発生元</param>
		/// <param name="e">イベントデータ</param>
		private void btnClearEntry_Click(object sender, EventArgs e)
		{
			OnClearEntryButton();
		}

		/// <summary>
		/// クリアキー入力時の処理
		/// </summary>
		/// <param name="sender">イベントの発生元</param>
		/// <param name="e">イベントデータ</param>
		private void btnClear_Click(object sender, EventArgs e)
		{
			OnClearButton();
		}

		/// <summary>
		/// 桁下げキー入力時の処理
		/// </summary>
		/// <param name="sender">イベントの発生元</param>
		/// <param name="e">イベントデータ</param>
		private void btnBack_Click(object sender, EventArgs e)
		{
			OnBackspaceButton();
		}

		/// <summary>
		///サインチェンジキー入力時の処理
		/// </summary>
		/// <param name="sender">イベントの発生元</param>
		/// <param name="e">イベントデータ</param>
		private void btnTogglesign_Click(object sender, EventArgs e)
		{
			OnToggleSignButton();
		}

		/// <summary>
		/// 最前面表示キー入力時の処理
		/// </summary>
		/// <param name="sender">イベントの発生元</param>
		/// <param name="e">イベントデータ</param>
		private void btnTopMost_Click(object sender, EventArgs e)
		{
			// フォームのTopMostプロパティを切り替える
			this.TopMost = !this.TopMost;

			// ボタンの色を更新するメソッドを呼び出す
			UpdateTopMostButtonColor();
		}

		/// <summary>
		/// 数字キー入力のメイン処理。
		/// ユーザーが押した数字を、現在の電卓の状態に応じて処理
		/// </summary>
		/// <param name="digit">入力された数字を表す文字列</param>
		private void OnDigitButton(string digit)
		{
			HandleInitialState();
			SetButtonsEnabled(true);
			m_lastActionWasPercent = false;

			//演算子が入力された後の式の表示欄の更新
			if (m_textOverwrite && m_currentOperatorType != OperatorType.NON && !string.IsNullOrEmpty(textExpression.Text))
			{
				var expr = textExpression.Text.Trim();
				var opSym = GetOperatorSymbol(m_currentOperatorType);
				if (expr.IndexOf(opSym, StringComparison.Ordinal) >= 0)
				{
					string left = FormatNumberForExpression(m_firstValue);
					textExpression.Text = string.Format("{0} {1}", left, opSym);
				}
			}

			//指数表示モードの場合は入力をリセット
			if (IsExponentDisplay())
			{
				m_textOverwrite = true;
				m_numDot = false;
				m_lastUserTypedRaw = "0";
			}

			//入力の妥当性を確認
			var currentRaw = m_lastUserTypedRaw;
			if (!IsInputValid(currentRaw, digit))
			{
				return;
			}

			//新しい入力か、既存への追加かを判断
			if (m_textOverwrite)
			{
				StartNewNumber(digit);
			}
			else
			{
				AppendDigit(digit);
			}

			//入力文字列を内部に保持
			decimal dv;
			if (decimal.TryParse(m_lastUserTypedRaw, NumberStyles.Any, CultureInfo.InvariantCulture, out dv))
			{
				m_displayValue = dv;
			}

			if (m_clearExpressionOnNextDigit)
			{
				textExpression.Text = "";
				m_clearExpressionOnNextDigit = false; // 一度使ったらリセット
			}


			textResult.Text = InsertCommasIfNeeded(m_lastUserTypedRaw, m_numDot);
			m_isClearEntry = false;
			m_preserveFormatOnToggle = true;
		}

		/// <summary>
		/// 小数点入力（"."）時の処理。
		/// 初期状態を整え、重複入力を防止し、
		/// 上書き開始中は "0." をセット、編集中は末尾に追加する。
		/// 内部の <see cref="m_displayValue"/> を更新し、
		/// 小数点フラグ <see cref="m_numDot"/> を true にする。
		/// </summary>
		private void OnDotButton()
		{
			HandleInitialState();
			m_lastActionWasPercent = false;

			if (m_numDot)
			{
				return;
			}

			//新しい数値の入力は0.をセット　既存の場合は.を追加
			if (m_textOverwrite)
			{
				m_lastUserTypedRaw = "0.";
				textResult.Text = m_lastUserTypedRaw;
				m_textOverwrite = false;
			}
			else
			{
				m_lastUserTypedRaw += ".";
				textResult.Text = m_lastUserTypedRaw;
			}
			m_numDot = true;

			var stringToParse = m_lastUserTypedRaw;
			if (stringToParse == "0.")
			{
				stringToParse = "0";
			}


			decimal dv;
			if (decimal.TryParse(stringToParse, NumberStyles.Any, CultureInfo.InvariantCulture, out dv))
			{
				m_displayValue = dv;
			}

			m_preserveFormatOnToggle = true;
		}

		/// <summary>
		/// 演算子キー入力のメイン処理。
		/// </summary>
		private void OnOperatorButton(OperatorType op)
		{
			if (IsError())
			{
				ResetCalculatorState();
				return;
			}

			try
			{
				// CE直後に演算子が押された場合の処理
				if (HandleClearEntryThenOperator(op))
				{
					return;
				}

				// 式の末尾にある演算子を置き換える処理
				if (TryReplaceTrailingOperator(op))
				{
					return;
				}
				// イコール直後に新しい演算子が押された場合の処理
				if (StartNewChainAfterEqual(op))
				{
					return;
				}
				//パーセントキー直後に演算子が押された場合の処理
				if (HandlePercentThenOperator(op))
				{
					return;
				}
				// 右辺未入力のまま演算子を変更する処理
				if (ChangeOperatorWhenRhsMissing(op))
				{
					return;
				}
				//通常の演算子入力
				if (ComputeThenSetNewOperator(op))
				{
					return;
				}
			}
			catch (OverflowException)
			{
				SetErrorState(Constants.ErrorMessage.OVERFLOW);
			}
		}

		/// <summary>
		/// クリアエントリー入力後に演算子キーが押されたとき、
		/// 演算子を設定して式を更新する処理。
		/// </summary>
		/// <param name="op">新しく入力された演算子</param>
		/// <returns>処理が実行された場合は true、そうでない場合は false</returns>
		private bool HandleClearEntryThenOperator(OperatorType op)
		{
			if (!m_isClearEntry)
			{
				return false;
			}

			m_currentOperatorType = op;
			textExpression.Text = string.Format("{0} {1}",
				FormatNumberForExpression(m_firstValue),
				GetOperatorSymbol(m_currentOperatorType));

			DisplayNumber(m_firstValue, true);

			m_isClearEntry = false;
			m_textOverwrite = true;
			m_numDot = false;
			m_lastActionWasPercent = false;
			m_preserveFormatOnToggle = false;
			m_lockRhsAfterAutoOp = false;
			return true;
		}

		/// <summary>
		/// 式の末尾にある既存の演算子を、入力された演算子に置き換える
		/// </summary>
		/// <param name="op">演算子</param>
		/// <returns>置き換えが成功した場合は true、そうでない場合は false</returns>
		private bool TryReplaceTrailingOperator(OperatorType op)
		{
			var curExpr = (textExpression.Text == null ? "" : textExpression.Text).Trim();

			// 式が空でない、演算子が末尾にある、＝で終わっていない場合のみ処理
			if (!(m_textOverwrite && curExpr.Length > 0 && !curExpr.EndsWith(Constants.Symbol.EQUAL)))
			{
				return false;
			}

			string[] ops = { Constants.Symbol.ADD, Constants.Symbol.SUBTRACT, Constants.Symbol.MULTIPLY, Constants.Symbol.DIVIDE };
			foreach (var o in ops)
			{
				if (curExpr.EndsWith(o))
				{
					textExpression.Text = curExpr.Substring(0, curExpr.Length - o.Length) + GetOperatorSymbol(op);
					m_currentOperatorType = op;

					m_lockRhsAfterAutoOp = false;
					m_lastActionWasPercent = false;
					m_textOverwrite = true;
					m_numDot = false;
					return true;
				}
			}
			return false;
		}

		/// <summary>
		/// イコールで計算を終えた後に、新しい演算子を入力した場合の処理
		/// </summary>
		/// <param name="op">演算子</param>
		/// <returns>新しい計算が開始された場合は true、そうでない場合は false</returns>
		private bool StartNewChainAfterEqual(OperatorType op)
		{
			if (!ExpressionEndsWithEqual())
			{
				return false;
			}

			// 現在の値を左辺に設定し、右辺は初期化
			m_firstValue = GetCurrentValue();
			m_secondValue = Constants.Numeric.INITIAL_VALUE;
			m_currentOperatorType = op;

			UpdateExpressionDisplay(m_firstValue, m_currentOperatorType);
			DisplayNumber(m_firstValue, true);

			m_textOverwrite = true;
			m_numDot = false;
			m_lastActionWasPercent = false;
			m_preserveFormatOnToggle = false;
			m_lockRhsAfterAutoOp = false;
			return true;
		}

		/// <summary>
		/// パーセントキーの直後に演算子キーが入力された場合の処理
		/// </summary>
		/// <param name="op">演算子</param>
		/// <returns>処理が成功した場合は true、エラーが発生した場合も true、それ以外は false</returns>
		private bool HandlePercentThenOperator(OperatorType op)
		{
			if (!(m_lastActionWasPercent && m_currentOperatorType != OperatorType.NON))
			{
				return false;
			}

			var cur = GetCurrentValue();
			PerformPendingCalculation(cur);
			if (IsError())
			{
				return true;
			}

			DisplayNumber(m_firstValue, true);
			m_currentOperatorType = op;
			UpdateExpressionDisplay(m_firstValue, m_currentOperatorType);

			m_textOverwrite = true;
			m_numDot = false;
			m_lastActionWasPercent = false;
			m_preserveFormatOnToggle = false;
			m_lockRhsAfterAutoOp = false;
			return true;
		}

		/// <summary>
		/// 演算子が入力されているが、右辺の値がまだ入力されていない状態で、演算子を変更した場合の処理。
		/// 例：「10 + 」の後に「-」を押して演算子を置き換える
		/// </summary>
		/// <param name="op">演算子</param>
		/// <returns>演算子が変更された場合は true、そうでない場合は false</returns>
		private bool ChangeOperatorWhenRhsMissing(OperatorType op)
		{
			if (!(m_textOverwrite && m_currentOperatorType != OperatorType.NON))
			{
				return false;
			}

			m_currentOperatorType = op;
			UpdateExpressionDisplay(m_firstValue, m_currentOperatorType);

			m_textOverwrite = true;
			m_numDot = false;
			m_lastActionWasPercent = false;
			m_preserveFormatOnToggle = false;
			m_lockRhsAfterAutoOp = false;
			return true;
		}

		/// <summary>
		/// 通常の演算子キー入力処理
		/// </summary>
		/// <param name="op">演算子</param>
		/// <returns>処理が成功した場合は true、エラーが発生した場合も true、それ以外は false</returns>
		private bool ComputeThenSetNewOperator(OperatorType op)
		{
			var currentValue = GetCurrentValue();
			PerformPendingCalculation(currentValue);
			if (IsError())
			{
				return true;
			}

			DisplayNumber(m_firstValue, true);
			m_currentOperatorType = op;
			UpdateExpressionDisplay(m_firstValue, m_currentOperatorType);

			m_textOverwrite = true;
			m_numDot = false;
			m_lastActionWasPercent = false;
			m_preserveFormatOnToggle = false;
			m_lockRhsAfterAutoOp = false;
			return true;
		}



		/// <summary>
		/// イコールキーのメイン処理
		/// 保留中の計算を最終確定し、結果を表示 
		/// </summary>
		private void OnEqualsButton()
		{
			if (ShouldResetOnError())
			{
				SetButtonsEnabled(true);
				return;
			}

			try
			{
				var result = ProcessEqualsLogic();
				if (IsError())
				{
					return;
				}

				// 判定ロジックを呼び出す
				if (ShouldUseExponentialNotation(result))
				{
					// 指数表記で表示
					 FormatNumberForDisplay(result);
				}
				else
				{
					// 通常表記で表示
					DisplayNumber(result, true);
				}

				m_preserveFormatOnToggle = false;
				m_lastActionWasPercent = false;

			}
			catch (InvalidOperationException ex)
			{
				SetErrorState(ex.Message);
			}
			catch (OverflowException)
			{
				SetErrorState(Constants.ErrorMessage.OVERFLOW);
			}
		}

		private bool ShouldUseExponentialNotation(decimal value)
		{
			// 割り切れる場合は通常表記
			if (value % 1 == 0)
			{
				return false;
			}

			// ゼロに近い値
			if (Math.Abs(value) < 0.000000000000001m && Math.Abs(value) != 0) // 1e-15未満の非常に小さい値
			{
				return true;
			}

			// 非常に大きい値
			if (Math.Abs(value) > 9999999999999999m) // 16桁を超える値
			{
				return true;
			}

			return false;
		}

		/// <summary>
		/// '%'キーが押されたときのイベントハンドラ。
		/// 計算機の現在の状態に基づいて、
		/// パーセント計算を行い、画面の表示を更新
		/// </summary>
		private void OnPercentButton()
		{
			if (ShouldResetOnError())
			{
				return;
			}

			var expr = (textExpression.Text != null ? textExpression.Text.Trim() : string.Empty);
			decimal dummyResult;
			var isOnlyNumber = string.IsNullOrEmpty(expr) || decimal.TryParse(expr, out dummyResult);

			// 演算子が未設定で、式が数値のみ、かつ直前が％でない場合 → 0を表示して終了
			if (m_currentOperatorType == OperatorType.NON && isOnlyNumber && !m_lastActionWasPercent)
			{
				DisplayZeroResult();
				textExpression.Text = Constants.Numeric.ZERO_VALUE;
				m_lastActionWasPercent = false;
				m_preserveFormatOnToggle = false;
				m_lockRhsAfterAutoOp = false;
				m_inPercentChainAfterEqual = false;
				m_percentChainFactor = 0m;
				return;
			}

			// 「＝」直後に％が押された場合の処理
            if (ExpressionEndsWithEqual())
			{
				var r = GetCurrentValue();

				// 加算・減算の場合は、r × (r × 0.01) を計算
				if (m_currentOperatorType == OperatorType.ADD || m_currentOperatorType == OperatorType.SUBTRACT)
				{
					m_percentChainFactor = r * Constants.Numeric.PERCENT_MULTIPLY; 
					var v = r * m_percentChainFactor;   

					m_firstValue = v;
					m_secondValue = Constants.Numeric.INITIAL_VALUE;
					m_currentOperatorType = OperatorType.NON;

					DisplayNumber(v, true);
					textExpression.Text = FormatNumberForExpression(v);

					m_lastActionWasPercent = true;
					m_preserveFormatOnToggle = false;
					m_lockRhsAfterAutoOp = true;
					m_inPercentChainAfterEqual = true;  
					return;
				}
				// 乗算・除算の場合は通常の％計算（r × 0.01）
				else
				{
					var v = CalculatePercent(r);

					m_firstValue = v;
					m_secondValue = Constants.Numeric.INITIAL_VALUE;
					m_currentOperatorType = OperatorType.NON;

					DisplayNumber(v, true);
					textExpression.Text = FormatNumberForExpression(v);

					m_lastActionWasPercent = true;
					m_preserveFormatOnToggle = false;
					m_lockRhsAfterAutoOp = true;
					m_inPercentChainAfterEqual = false;
					m_percentChainFactor = 0m;
					return;
				}
			}

			// イコール直後に％を連打している場合の処理
			if (m_currentOperatorType == OperatorType.NON && m_inPercentChainAfterEqual && m_percentChainFactor != 0m)
			{
				var cur = GetCurrentValue();          
				var v = cur * m_percentChainFactor;     

				m_firstValue = v;
				m_secondValue = Constants.Numeric.INITIAL_VALUE;

				DisplayNumber(v, true);
				textExpression.Text = FormatNumberForExpression(v);

				m_lastActionWasPercent = true;
				m_preserveFormatOnToggle = false;
				m_lockRhsAfterAutoOp = true;
				return;
			}

			//演算子後の％計算
			var rhsSource =
				(m_lastActionWasPercent && m_currentOperatorType != OperatorType.NON)
					? GetCurrentValue()
					: (m_textOverwrite ? m_firstValue : GetCurrentValue());
			var percent = CalculatePercent(rhsSource);

			// 加算・減算 → 左辺 × パーセント
			decimal replacedB;
			if (m_currentOperatorType == OperatorType.ADD || m_currentOperatorType == OperatorType.SUBTRACT)
			{
				replacedB = m_firstValue * percent;
			}
			// 乗算・除算 → rhs × 0.01
			else
			{
				replacedB = percent;
			}

			textExpression.Text = string.Format("{0} {1} {2}",
				FormatNumberForExpression(m_firstValue),
				GetOperatorSymbol(m_currentOperatorType),
				FormatNumberForExpression(replacedB));

			DisplayNumber(replacedB, true);

			m_lastActionWasPercent = true;
			m_preserveFormatOnToggle = false;
			m_lockRhsAfterAutoOp = true;
			m_inPercentChainAfterEqual = false;
			m_percentChainFactor = 0m;
		}

		/// <summary>
		/// クリアエントリーキー入力の処理。式は維持し、表示を 0 に戻す。
		/// </summary>
		private void OnClearEntryButton()
		{
			if (ShouldResetOnError())
			{
				SetButtonsEnabled(true);
				return;
			}

			var currentExpression = textExpression.Text != null ? textExpression.Text.Trim() : string.Empty;

			// 直前に％キーで右辺を置き換えた状態でCEを押した場合：
			// 式を「A 演算子」に戻し、表示欄は「0」にする
			if (!ExpressionEndsWithEqual() && m_currentOperatorType != OperatorType.NON && m_lastActionWasPercent)
			{
				DisplayZeroResult();
				textExpression.Text = string.Format("{0} {1}",
					FormatNumberForExpression(m_firstValue),
					GetOperatorSymbol(m_currentOperatorType));
				m_lastActionWasPercent = false;
				m_lockRhsAfterAutoOp = false;
				return;
			}

			// negate（±）などで単独の結果を表示している状態でCEを押した場合：
			// 式も表示もすべて消して「0」に戻す
			if (m_lockRhsAfterAutoOp && m_currentOperatorType == OperatorType.NON && !ExpressionEndsWithEqual())
			{
				DisplayZeroResult();
				textExpression.Text = "";
				m_lockRhsAfterAutoOp = false;
				return;
			}

			// イコールで計算が終わった後にCEを押した場合
			if (ExpressionEndsWithEqual())
			{
				//演算子がある場合リセット
				if (HasBinaryOperatorInExpression(currentExpression))
				{
					ResetCalculatorState();
					SetButtonsEnabled(true);
					return;
				}

				//演算子がない場合結果表示だけリセット
				DisplayZeroResult();
				ResetCalculationValues();
				return;
			}

			ClearCurrentEntry();
		}

		/// <summary>
		/// 数式列に演算子が含まれているかを判定
		/// ' = '記号より前の部分のみを検査し、加算、減算、乗算、除算のいずれかが含まれていればtrueを返す
		/// </summary>
		/// <param name="expr">検査対象となる数式</param>
		/// <returns>演算子が含まれていればtrue、そうでなければfalse</returns>
		private bool HasBinaryOperatorInExpression(string expr)
		{
			if (string.IsNullOrEmpty(expr))
			{
				return false;
			}

			int eq = expr.LastIndexOf(Constants.Symbol.EQUAL);
			var body = (eq >= 0) ? expr.Substring(0, eq) : expr;

			return body.Contains(" " + Constants.Symbol.ADD + " ") ||
			   body.Contains(" " + Constants.Symbol.SUBTRACT + " ") ||
			   body.Contains(" " + Constants.Symbol.MULTIPLY + " ") ||
			   body.Contains(" " + Constants.Symbol.DIVIDE + " ");
		}

		/// <summary>
		/// 電卓の表示を'0'に設定
		/// </summary>
		private void DisplayZeroResult()
		{
			textResult.Text = Constants.Numeric.ZERO_VALUE;
			m_textOverwrite = true;
			m_numDot = false;

			// 内部もゼロに同期
			m_displayValue = 0m;
			m_lastUserTypedRaw = "0";

			m_preserveFormatOnToggle = false;
			m_lastActionWasPercent = false;
		}

		/// <summary>
		/// 計算状態を初期値に戻す
		/// </summary>
		private void ResetCalculationValues()
		{
			m_firstValue = Constants.Numeric.INITIAL_VALUE;
			m_secondValue = Constants.Numeric.INITIAL_VALUE;
			m_currentOperatorType = OperatorType.NON;
		}

		/// <summary>
		/// 現在の数値をクリアし、表示を0に戻す
		/// </summary>
		private void ClearCurrentEntry()
		{
			m_isClearEntry = true;
			DisplayZeroResult();
		}

		/// <summary>
		/// クリアキーの処理。全状態を初期化する。
		/// </summary>
		private void OnClearButton()
		{
			ResetAllState();
		}

		/// <summary>
		/// 桁下げキー入力の処理
		/// 末尾1文字削除を行う。
		/// </summary>
		private void OnBackspaceButton()
		{
			if (ShouldResetOnError())
			{
				SetButtonsEnabled(true);
				return;
			}

			// 「＝」で計算が終わった直後に ← を押した場合 → 式を消して初期化
			if (ExpressionEndsWithEqual())
			{
				textExpression.Text = "";
				m_textOverwrite = true;
				m_numDot = false;
				m_preserveFormatOnToggle = false;
				m_lastActionWasPercent = false;
				m_clearedExprAfterEqual = true;

				m_lastUserTypedRaw = "0";
				m_displayValue = 0m;
				return;
			}

			// すでに「＝」後に式を消していた場合 → 何もしない
			if (m_clearedExprAfterEqual)
			{
				return;
			}

			// 指数表示モードの場合 → 入力を初期化
			if (IsExponentDisplay())
			{
				m_textOverwrite = true;
				m_numDot = false;
				textResult.Text = Constants.Numeric.ZERO_VALUE;

				m_lastUserTypedRaw = "0";
				m_displayValue = 0m;

				m_preserveFormatOnToggle = false;
				m_lastActionWasPercent = false;
				return;
			}

			if (m_textOverwrite)
			{
				return;
			}

			// 入力値がある場合 → 末尾1文字を削除
			if (m_lastUserTypedRaw.Length > 0)
			{
				var newRaw = m_lastUserTypedRaw.Substring(0, m_lastUserTypedRaw.Length - 1);
				if (string.IsNullOrEmpty(newRaw) || newRaw == "-")
				{
					m_lastUserTypedRaw = "0";
					m_textOverwrite = true;
					m_numDot = false;
				}
				else
				{
					m_lastUserTypedRaw = newRaw;
					m_numDot = m_lastUserTypedRaw.IndexOf(".", StringComparison.Ordinal) >= 0;
				}
			}
			else
			{
				ResetCalculatorState();
				return;
			}

			decimal dv;
			if (decimal.TryParse(m_lastUserTypedRaw, NumberStyles.Any, CultureInfo.InvariantCulture, out dv))
			{
				m_displayValue = dv;
			}

			textResult.Text = InsertCommasIfNeeded(m_lastUserTypedRaw, m_numDot);

			m_preserveFormatOnToggle = true;
			m_lastActionWasPercent = false;
		}

		private bool m_clearExpressionOnNextDigit = false;


		/// <summary>
		/// サインチェンジキーの処理。ユーザー入力の見た目保持と、
		/// ＝直後の negate表記更新に対応する。
		/// </summary>
		private void OnToggleSignButton()
		{
			if (ShouldResetOnError())
			{
				return;
			}

			var expr = (textExpression.Text != null ? textExpression.Text.Trim() : string.Empty);
			bool isNegateExpr = expr.StartsWith(Constants.SpecialDisplay.NEGATE_FUNCTION + "(", StringComparison.Ordinal);

			//イコール直後またはnegate()表記中の場合現在地を反転させ途中計算表示欄更新
			if (ExpressionEndsWithEqual() || isNegateExpr)
			{
				decimal v = -GetCurrentValue();
				DisplayNumber(v, true);
				UpdateExpressionForToggleSign();

				if (textExpression.Text.StartsWith(Constants.SpecialDisplay.NEGATE_FUNCTION + "(", StringComparison.Ordinal))
				{
					m_firstValue = -Math.Abs(m_firstValue); // 絶対値を取ってから負の値にするのが確実
				}

				m_clearExpressionOnNextDigit = true; // 次の数値入力で式欄をクリアする
				m_preserveFormatOnToggle = false;
				m_lastActionWasPercent = false;
				m_lockRhsAfterAutoOp = true;   
				return;
			}

			// 数値　演算子 の直後（右辺未入力）の場合、式表示欄にnegateを追加
			if (m_currentOperatorType != OperatorType.NON && m_textOverwrite)
			{
				var a = m_firstValue;
				var b = -a;

				textExpression.Text = string.Format("{0} {1} {2}",
					FormatNumberForExpression(a),
					GetOperatorSymbol(m_currentOperatorType),
					string.Format("{0}({1})",
						Constants.SpecialDisplay.NEGATE_FUNCTION,
						FormatNumberForExpression(a)));

				m_displayValue = b;
				DisplayNumber(b, true);      
				m_preserveFormatOnToggle = false;
				m_lastActionWasPercent = false;
				m_lockRhsAfterAutoOp = true;    
				return;
			}

			m_displayValue = -GetCurrentValue();
			DisplayNumber(m_displayValue, false);
			m_preserveFormatOnToggle = false;
			m_lastActionWasPercent = false;
		}

		/// <summary>
		/// イコールキー入力直後の サインチェンジキーを入力したとき negate(...) の入れ子表記で更新する。
		/// 例）"100 =" → "negate(100) " → さらに ± → "negate(negate(100))"
		/// </summary>
		private void UpdateExpressionForToggleSign()
		{
			var expr = (textExpression.Text != null ? textExpression.Text.Trim() : string.Empty);

			//式がイコールで終わっている場合、negate(結果)に更新
			if (expr.EndsWith(Constants.Symbol.EQUAL))
			{

				string formattedResult = FormatNumberForExpression(m_firstValue);

				textExpression.Text = Constants.SpecialDisplay.NEGATE_FUNCTION + "(" + formattedResult + ")";
				return;
			}

			//式がすでにnegateである場合、入れ子にする
			if (expr.StartsWith(Constants.SpecialDisplay.NEGATE_FUNCTION + "(", StringComparison.Ordinal))
			{
				textExpression.Text = Constants.SpecialDisplay.NEGATE_FUNCTION + "(" + expr + ")";
			}
		}

		/// <summary>
		/// 最前面表示ボタンが入力されると色が変わる
		/// </summary>
		private void UpdateTopMostButtonColor()
		{
			if (this.TopMost)
			{
				// 最前面に固定された状態
				btnTopMost.BackColor = Color.LightBlue;
			}
			else
			{
				// 最前面が解除された状態
				btnTopMost.BackColor = SystemColors.Control;
			}
		}

		/// <summary>
		/// 新しい数値入力を開始する。上書きモードを解除し、小数点フラグを更新。
		/// </summary>
		/// <param name="digit">新しい数値の最初の桁を表す文字列。</param>
		private void StartNewNumber(string digit)
		{
			m_lastUserTypedRaw = digit;
			textResult.Text = digit;
			m_textOverwrite = false;
			m_numDot = (digit == ".");

			var stringToParse = m_lastUserTypedRaw;

			// "." 単体の場合は 0に置き換えて数値として扱えるようにする
			if (stringToParse == ".")
			{
				stringToParse = "0";
			}

			decimal dv;
			if (decimal.TryParse(stringToParse, NumberStyles.Any, CultureInfo.InvariantCulture, out dv))
			{
				m_displayValue = dv;
			}
		}

		/// <summary>
		/// 既存の入力の末尾に数字を追加する。
		/// </summary>
		/// <param name="digit">追加する数字を表す文字列。</param>
		private void AppendDigit(string digit)
		{
			m_lastUserTypedRaw += digit;
			textResult.Text = InsertCommasIfNeeded(m_lastUserTypedRaw, m_numDot);

			decimal dv;
			if (decimal.TryParse(m_lastUserTypedRaw, NumberStyles.Any, CultureInfo.InvariantCulture, out dv))
			{
				m_displayValue = dv;
			}
		}

		/// <summary>
		/// ユーザーの入力が有効であるか検証
		/// 最大表示桁数の超過、重複したゼロ、不適切な小数点の入力などをチェック
		/// </summary>
		/// <param name="currentText">現在の表示。</param>
		/// <param name="digit">新たに入力された数字または記号。</param>
		/// <returns>入力が有効であればtrue、そうでなければfalse。</returns>
		private bool IsInputValid(string currentText, string digit)
		{
			var startsWithZeroDot = currentText.StartsWith("0.") || currentText.StartsWith("-0.");
			var maxDigits = startsWithZeroDot ? Constants.Numeric.MAX_FRACTIONAL_DIGITS : Constants.Numeric.MAX_DISPLAY_DIGITS;

			var nextText = m_textOverwrite ? digit : currentText + digit;
			var nextLength = nextText.Replace(".", "").Replace("-", "").Length;

			if (nextLength > maxDigits)
			{
				return false;
			}

			if (!m_textOverwrite && currentText == Constants.Numeric.ZERO_VALUE && digit == Constants.Numeric.ZERO_VALUE && !m_numDot)
			{
				return false;
			}

			return true;
		}

		/// <summary>
		/// 指定された演算子に基づき、演算子の記号を返す。
		/// </summary>
		/// <param name="type">演算子</param>
		/// <returns>指定された演算子に対応する記号</returns>
		private string GetOperatorSymbol(OperatorType type)
		{
			switch (type)
			{
				case OperatorType.ADD:
					return Constants.Symbol.ADD;
				case OperatorType.SUBTRACT:
					return Constants.Symbol.SUBTRACT;
				case OperatorType.MULTIPLY:
					return Constants.Symbol.MULTIPLY;
				case OperatorType.DIVIDE:
					return Constants.Symbol.DIVIDE;
				default:
					return string.Empty;
			}
		}

		/// <summary>
		/// 左辺と右辺の値を指定された演算子タイプに基づいて計算する
		/// </summary>
		/// <param name="left">左辺の値</param>
		/// <param name="right">右辺の値</param>
		/// <param name="type">演算子のタイプ</param>
		/// <returns>計算結果</returns>
		private decimal Calculate(decimal left, decimal right, OperatorType type)
		{
			switch (type)
			{
				case OperatorType.ADD:
					return left + right;
				case OperatorType.SUBTRACT:
					return left - right;
				case OperatorType.MULTIPLY:
					return left * right;
				case OperatorType.DIVIDE:
					return left / right;
				default:
					return right;
			}
		}

		/// <summary>
		/// 除算処理を行い、結果またはエラーコードを返す。
		/// </summary>
		private static ErrorCode Divide(decimal numerator, decimal denominator, out decimal result)
		{
			if (denominator == 0)
			{
				result = 0;
				return numerator == 0 ? ErrorCode.Undefined : ErrorCode.DivideByZero;
			}

			result = numerator / denominator;
			return ErrorCode.Success;
		}


		/// <summary>
		/// 保留中の演算を実行する処理。
		/// 演算子が未選択なら、左辺に現在の値をセットするだけ。
		/// 割り算の場合は、0除算や 0÷0 のようなエラーを検出して処理する。
		/// </summary>
		private void PerformPendingCalculation(decimal currentValue)
		{
			// イコールが押された直後、または演算子が未選択の場合
			// 現在の値を左辺として保持するだけで計算はしない
			if (ExpressionEndsWithEqual() || m_currentOperatorType == OperatorType.NON)
			{
				m_firstValue = currentValue;
			}
			else
			{
				//演算子が÷の場合は0除算、0÷0のチェック
				if (m_currentOperatorType == OperatorType.DIVIDE)
				{
					decimal divResult;
					ErrorCode code = Divide(m_firstValue, currentValue, out divResult);
					if (code == ErrorCode.Undefined)
					{
						SetErrorState(Constants.ErrorMessage.UNDEFINED);
						return;
					}
					if (code == ErrorCode.DivideByZero)
					{
						SetErrorState(Constants.ErrorMessage.DIVIDE_BY_ZERO);
						return;
					}
					m_firstValue = divResult;
				}
				else
				{
					decimal result = Calculate(m_firstValue, currentValue, m_currentOperatorType);
					m_firstValue = result;
				}
			}
		}

		/// <summary>
		/// 途中式表示を更新する
		/// </summary>
		private void UpdateExpressionDisplay(decimal value, OperatorType type)
		{
			var op = GetOperatorSymbol(type);

			var expr = (textExpression.Text != null ? textExpression.Text.Trim() : string.Empty);

			// 式が negate() で始まっている場合  そのまま演算子を追加して終了
			if (expr.StartsWith(Constants.SpecialDisplay.NEGATE_FUNCTION + "(", StringComparison.Ordinal))
			{
				textExpression.Text = string.Format("{0} {1}", expr, op);
				return;
			}

			string displayStr;
			var abs = Math.Abs(value);

			// 非ゼロかつ非常に小さい or 非常に大きい場合 → 指数表記に変換
			if (abs != 0m && (abs < Constants.Numeric.SCI_SMALL_THRESHOLD || abs >= Constants.Numeric.SCI_LARGE_THRESHOLD))
			{
				displayStr = FormatExponential(value);
			}
			else
			{
				var rounded = RoundResult(value);
				displayStr = FormatNumberForDisplay(rounded);
			}

			// 式表示欄に「数値 演算子」の形式で表示
			textExpression.Text = string.Format("{0} {1}", displayStr, op);
		}

		/// <summary>
		///イコールキーの処理
		///保留中の演算を実行し、結果を表示欄と式欄に反映する。
		/// 割り算の場合は 0除算や未定義（0÷0）を検出してエラー表示する。
		/// </summary>
		private decimal ProcessEqualsLogic()
		{
			var currentValue = GetCurrentValue();
			var isFirstEqual = !ExpressionEndsWithEqual();

			// 演算子が未選択の場合  単独の値として「＝」を表示
			if (m_currentOperatorType == OperatorType.NON)
			{
				m_secondValue = currentValue;
				textExpression.Text = string.Format("{0} {1}",
					FormatNumberForExpression(currentValue), Constants.Symbol.EQUAL);
				m_firstValue = currentValue;
				return currentValue;
			}

			// イコールキーを初めて押したときは、右辺に現在の入力値を使って計算する。
			decimal left, right;
			if (isFirstEqual)
			{
				left = m_firstValue;
				right = currentValue;
				m_secondValue = currentValue;
			}
			// 2回目以降のイコールでは、前回使った右辺の値を再利用して繰り返し計算する。
			else
			{
				left = m_firstValue;
				right = m_secondValue;
			}

			decimal result;

			// 演算子が「÷」の場合 → 0除算や未定義をチェック
			if (m_currentOperatorType == OperatorType.DIVIDE)
			{
				decimal divResult;
				ErrorCode code = Divide(left, right, out divResult);  
				if (code == ErrorCode.Undefined)
				{
					SetErrorState(Constants.ErrorMessage.UNDEFINED);
					return m_firstValue;   
				}
				if (code == ErrorCode.DivideByZero)
				{
					SetErrorState(Constants.ErrorMessage.DIVIDE_BY_ZERO);
					return m_firstValue;   
				}
				result = divResult;
			}
			else
			{
				result = Calculate(left, right, m_currentOperatorType);
			}

			m_firstValue = result;

			var opSym = GetOperatorSymbol(m_currentOperatorType);
			var leftExpr = FormatNumberForExpression(left);
			var rightExpr = FormatNumberForExpression(right);

			var curr = (textExpression.Text != null ? textExpression.Text.Trim() : string.Empty);

			// negate() で始まる式の場合 → 入れ子構造を維持して「＝」を追加
			if (!string.IsNullOrEmpty(curr) &&
				!curr.EndsWith(Constants.Symbol.EQUAL) &&
				curr.StartsWith(Constants.SpecialDisplay.NEGATE_FUNCTION + "(", StringComparison.Ordinal))
			{
				if (curr.EndsWith(opSym))
					textExpression.Text = curr + " " + rightExpr + " " + Constants.Symbol.EQUAL;
				else
					textExpression.Text = curr + " " + Constants.Symbol.EQUAL;
			}
			else
			{
				textExpression.Text = string.Format("{0} {1} {2} {3}",
					leftExpr, opSym, rightExpr, Constants.Symbol.EQUAL);
			}

			return result;
		}

		/// <summary>
		/// 現在値を％形式に変換する。
		/// </summary>
		private decimal CalculatePercent(decimal value)
		{
			return value * Constants.Numeric.PERCENT_MULTIPLY;
		}

		private void DisplayNumber(decimal value, bool overwrite)
		{
			string s;

			var rounded = RoundResult(value);
			s = FormatNumberForDisplay(rounded);

			textResult.Text = s;
			if (!IsExponentDisplay()) UpdateTextResultWithCommas();

			m_textOverwrite = overwrite;
			m_numDot = false;

			m_displayValue = value;

			m_preserveFormatOnToggle = false;
			m_lastUserTypedRaw = IsExponentDisplay()
				? m_displayValue.ToString("0.#############################", CultureInfo.InvariantCulture)
				: textResult.Text.Replace(",", "");
		}

		/// <summary>
		/// カンマ区切りのための書式化を行う
		/// </summary>
		/// <param name="raw">編集中の未加工テキスト</param>
		/// <param name="numDot">小数点入力済みフラグ</param>
		/// <returns>整形後テキスト</returns>
		private string InsertCommasIfNeeded(string raw, bool numDot)
		{
			if (string.IsNullOrEmpty(raw) || raw == "-" || (raw == "0" && !numDot)) return raw;

			var neg = raw.StartsWith("-");
			if (neg) raw = raw.Substring(1);

			var dot = raw.IndexOf('.');
			var intPart = dot >= 0 ? raw.Substring(0, dot) : raw;
			var fracPart = dot >= 0 ? raw.Substring(dot + 1) : "";

			decimal iv;
			if (decimal.TryParse(intPart, NumberStyles.Number, CultureInfo.InvariantCulture, out iv))
			{
				var intFmt = iv.ToString("#,##0", CultureInfo.InvariantCulture);
				var newText = (dot >= 0) ? (intFmt + "." + fracPart) : intFmt;
				if (neg) newText = "-" + newText;
				return newText;
			}
			return raw;
		}

		/// <summary>
		/// 結果表示欄に 3 桁区切りを反映する。
		/// </summary>
		private void UpdateTextResultWithCommas()
		{
			if (IsError()) return;
			if (IsExponentDisplay()) return;

			var raw = textResult.Text.Replace(",", "");
			var hasDot = raw.IndexOf(".", StringComparison.Ordinal) >= 0;
			var formatted = InsertCommasIfNeeded(raw, hasDot);

			if (formatted != textResult.Text)
			{
				int fromEnd = textResult.Text.Length - textResult.SelectionStart;
				textResult.Text = formatted;
				textResult.SelectionStart = Math.Max(0, textResult.Text.Length - fromEnd);
			}
		}

		private string FormatNumberForExpression(decimal value)
		{
			var rounded = RoundResult(value);
			return FormatNumberForDisplay(rounded);
		}

		//指数表記

		/// <summary>
		/// 指定された整数 k に対して、10 の k 乗を計算する。
		/// k が正なら 10 を k 回掛け、負なら 10 を k 回割る。
		/// </summary>
		/// <param name="k">指数（正または負）</param>
		private static decimal Pow10(int k)
		{
			if (k == 0) return 1m;
			var p = 1m;
			if (k > 0)
			{
				for (int i = 0; i < k; i++) p *= 10m;
			}
			else
			{
				for (int i = 0; i < -k; i++) p /= 10m;
			}
			return p;
		}

		/// <summary>
		/// 数値 x の 10 進指数（桁数）を求める。
		/// 整数部の桁数または小数点以下のゼロの数から指数を算出。
		/// </summary>
		/// <param name="x">対象の数値</param>
		/// <returns>10 進指数（整数）</returns>
		private static int DecimalBase10Exponent(decimal x)
		{
			var ax = Math.Abs(x);
			if (ax == 0m) return 0;

			if (ax >= 1m)
			{
				string s = decimal.Truncate(ax).ToString(CultureInfo.InvariantCulture);
				return s.Length - 1;
			}
			// 小数部のみの場合 → 小数点以下のゼロの数を数えて指数を負で返す
			else
			{
				var s = ax.ToString("0.#############################", CultureInfo.InvariantCulture);
				var dot = s.IndexOf('.');
				var zeros = 0;
				for (int i = dot + 1; i < s.Length && s[i] == '0'; i++) zeros++;
				return -(zeros + 1); 
			}
		}

		/// <summary>
		/// 指定した有効桁数で四捨五入する。
		/// 整数部／小数部のいずれで丸めるかを判定する。
		/// </summary>
		/// <param name="x">対象値</param>
		/// <param name="n">有効桁数</param>
		/// <returns>丸め後の値</returns>
		private static decimal RoundToSignificantDigits(decimal x, int n)
		{
			// 0 の場合はそのまま返す
			if (x == 0m)
			{
				return 0m;
			}

			var exp = DecimalBase10Exponent(x);
			var scale = n - 1 - exp;         // 丸める位置を計算          

			if (scale < 0)
			{
				var k = -scale;
				return Math.Round(x / Pow10(k), 0, MidpointRounding.AwayFromZero) * Pow10(k);
			}
			else
			{
				var safeScale = scale > 28 ? 28 : scale;
				var rounded = Math.Round(x, safeScale, MidpointRounding.AwayFromZero);
				return rounded;
			}
		}

		/// <summary>
		/// 表示用の丸めを行う。指数レンジでは事前丸めを行わない。
		/// </summary>
		/// <param name="value">対象値</param>
		/// <returns>丸め後の値</returns>
		private decimal RoundResult(decimal value)
		{
			var abs = Math.Abs(value);
			if (abs == 0m)
			{
				return 0m;
			}

			// 非常に小さい or 非常に大きい値 → 丸めずそのまま返す
			if (abs < Constants.Numeric.SCI_SMALL_THRESHOLD || abs >= Constants.Numeric.SCI_LARGE_THRESHOLD)
			{
				return value;
			}
				
			 // 小数部が多い場合 → 最大 16 桁まで丸める
			if (abs > 0m && abs < 1m)
			{
				return Math.Round(value, 16, MidpointRounding.AwayFromZero);
			}

			// 整数部の桁数に応じて小数部の丸める
			if (abs >= 1m)
			{
				var integerPartStr = Math.Floor(abs).ToString(CultureInfo.InvariantCulture);
				var integerLength = integerPartStr.Length;
				var decimalPlacesToRound = 16 - integerLength;
				if (decimalPlacesToRound >= 0)
				{
					return Math.Round(value, decimalPlacesToRound, MidpointRounding.AwayFromZero);
				}
					
			}
			return value;
		}

		/// <summary>
		/// 指数表示用の文字列を生成する。
		/// 仮数の整数値には末尾 '.' を付与する。
		/// </summary>
		/// <param name="value">対象値</param>
		/// <returns>指数表記の文字列</returns>
		private string FormatExponential(decimal value)
		{
			const int SIG = Constants.Numeric.EXP_SIGNIFICANT_DIGITS; // 16

			if (value == 0m)
			{
				return "0";
			}

			var rounded = RoundToSignificantDigits(value, SIG);

			var exp = DecimalBase10Exponent(rounded);
			var mant = rounded / Pow10(exp);

			// 仮数が 10 以上なら 1桁下げて指数を1増やす
			if (Math.Abs(mant) >= 10m)
			{
				mant /= 10m;
				exp += 1;
			}

			string mantStr;
			var mantAbsTrunc = decimal.Truncate(Math.Abs(mant));
			// 仮数が整数なら → 末尾に「.」を付ける
			if (Math.Abs(mant) == mantAbsTrunc)
			{
				mantStr = (mant >= 0 ? "" : "-") + mantAbsTrunc.ToString("0", CultureInfo.InvariantCulture) + ".";
			}
			else
			{
				mantStr = mant.ToString("0.#############################", CultureInfo.InvariantCulture).TrimEnd('0');
			}

			var expStr = (exp >= 0 ? "+" : "") + exp.ToString(CultureInfo.InvariantCulture);

			return mantStr + "e" + expStr;
		}

		/// <summary>
		/// 指定値を通常表示用の文字列に変換する。
		/// </summary>
		/// <param name="value">対象値</param>
		/// <returns>表示文字列</returns>
		private string FormatNumberForDisplay(decimal value)
		{
			var abs = Math.Abs(value);
			if (abs == 0m) return Constants.Numeric.ZERO_VALUE;

			// 絶対値が閾値以上なら指数表記
			if (abs < Constants.Numeric.SCI_SMALL_THRESHOLD || abs >= Constants.Numeric.SCI_LARGE_THRESHOLD)
			{
				return FormatExponential(value);
			}
				

			string fixedStr = value.ToString("0.#############################", CultureInfo.InvariantCulture);

			//小数の処理
			if (abs < 1m)
			{
				var dot = fixedStr.IndexOf('.');               
				var leadingZeros = 0;
				for (int i = dot + 1; i < fixedStr.Length && fixedStr[i] == '0'; i++)
				{
					leadingZeros++;
				}

				var significantDigits = 0;
				for (int i = dot + 1 + leadingZeros; i < fixedStr.Length; i++)
					if (char.IsDigit(fixedStr[i]))
					{
						significantDigits++;
					}

				if (significantDigits <= Constants.Numeric.MAX_SIGNIFICANT_DIGITS)
				{
					return fixedStr;
				}
				
				// 有効桁数が多すぎる場合は、指数表記に切り替える
				if (abs < Constants.Numeric.SCI_SMALL_THRESHOLD)
				{
					return FormatExponential(value);
				}

				return FormatExponential(value);
			}

			// 整数（1以上）の値に対する表示形式の判定処理。
			// 符号の有無を確認し、整数部の桁数を計算。
			// 表示可能な最大桁数（例：12桁）を超えている場合は指数表記に切り替える。
			bool neg = fixedStr[0] == '-';
			int dot2 = fixedStr.IndexOf('.');                  
			int intLen = (dot2 >= 0 ? dot2 : fixedStr.Length) - (neg ? 1 : 0);
			if (intLen > Constants.Numeric.MAX_DISPLAY_DIGITS)
				return FormatExponential(value);

			return fixedStr;
		}

		/// <summary>
		/// 現在の内部表示値（DisplayValue）を取得する。
		/// </summary>
		/// <returns>現在値</returns>
		private decimal GetCurrentValue()
		{
			return m_displayValue;
		}

		

		/// <summary>
		/// 現在がエラー状態かどうかを返す。
		/// </summary>
		/// <returns>エラー状態なら true</returns>
		private bool IsError()
		{
			return m_isErrorState;
		}

		/// <summary>
		/// 途中式表示が '=' で終わっているかどうかを返す。
		/// </summary>
		/// <returns>'=' 終了なら true</returns>
		private bool ExpressionEndsWithEqual()
		{
			return textExpression.Text.Length > 0 && textExpression.Text.EndsWith(Constants.Symbol.EQUAL);
		}

		/// <summary>
		/// 現在の結果表示が指数表記かどうかを返す。
		/// </summary>
		/// <returns>指数表記なら true</returns>
		private bool IsExponentDisplay()
		{
			var t = textResult.Text;
			return (t.IndexOf('e') >= 0 || t.IndexOf('E') >= 0);
		}

		/// <summary>
		/// エラー時に必要なリセット処理を行い、処理継続可否を返す。
		/// </summary>
		/// <returns>リセットを行った場合は true</returns>
		private bool ShouldResetOnError()
		{
			if (m_isErrorState)
			{
				ResetCalculatorState();
				return true;
			}
			return false;
		}

		/// <summary>
		/// 結果表示欄の文字幅に合わせて自動でフォントサイズを縮小する。
		/// </summary>
		private void AutoFitResultFont()
		{
			var fontSize = m_defaultFontSize;   
			FontFamily family = textResult.Font.FontFamily;
			FontStyle style = textResult.Font.Style;

			while (fontSize > Constants.FontSize.MIN_LIMIT)
			{
				using (Font trialFont = new Font(family, fontSize, style))  
				{
					Size trialtextSize = new Size(int.MaxValue, int.MaxValue);
					TextFormatFlags flags = TextFormatFlags.NoPadding | TextFormatFlags.SingleLine;
					Size trialTextSize = TextRenderer.MeasureText(
						textResult.Text,
						trialFont,
						trialtextSize,
						flags
					); 

					if (trialTextSize.Width <= textResult.ClientSize.Width)
					{
						if (Math.Abs(textResult.Font.Size - fontSize) > Constants.FontSize.SIZE_EPSILON)
						{
							Font oldFont = textResult.Font;
							textResult.Font = new Font(family, fontSize, style);
							oldFont.Dispose();
						}
						return;
					}
				}
				fontSize -= Constants.FontSize.SIZE_EPSILON;
			}

			if (Math.Abs(textResult.Font.Size - Constants.FontSize.MIN_LIMIT) > Constants.FontSize.REDUCTION_STEP)
			{
				Font oldFinalFont = textResult.Font;
				textResult.Font = new Font(family, Constants.FontSize.MIN_LIMIT, style);
				oldFinalFont.Dispose();
			}
		}


		/// <summary>
		/// エラー時に無効化する対象キーの有効/無効を設定する。
		/// </summary>
		/// <param name="enabled">有効にするか</param>
		private void SetButtonsEnabled(bool enabled)
		{
			foreach (Button btn in m_disabledButtonsOnError) btn.Enabled = enabled;
		}

		/// <summary>
		/// エラー状態に遷移し、エラーメッセージやフォントサイズ・ボタン状態を更新する。
		/// </summary>
		/// <param name="message">表示するエラーメッセージ</param>
		private void SetErrorState(string message)
		{
			textResult.Text = message;

			var fontSize = Constants.FontSize.ERROR_MESSAGE;
			textResult.Font = new Font(textResult.Font.FontFamily, fontSize, textResult.Font.Style);

			m_isErrorState = true;
			SetButtonsEnabled(false);
		}

		/// <summary>
		/// エラーまたは '=' 直後に必要な初期化を行う。
		/// </summary>
		private void HandleInitialState()
		{
			if (m_isErrorState || ExpressionEndsWithEqual())
			{
				ResetCalculatorState();
				m_clearedExprAfterEqual = false;
			}
		}

		/// <summary>
		/// すべての状態を初期化する。
		/// </summary>
		private void ResetAllState()
		{
			ResetCalculatorState();
			SetButtonsEnabled(true);		}


		/// <summary>
		/// 電卓の内部状態を初期値に戻す。
		/// </summary>
		private void ResetCalculatorState()
		{
			InitializeValues();
			ClearTextFields();
			ResetFlags();
			ResetFonts();
		}

		/// <summary>
		/// 数値レジスタ類（First/Second/Display など）を初期化する。
		/// </summary>
		private void InitializeValues()
		{
			m_firstValue = Constants.Numeric.INITIAL_VALUE;
			m_secondValue = Constants.Numeric.INITIAL_VALUE;
			m_currentOperatorType = OperatorType.NON;
			m_displayValue = 0m;
			m_lastUserTypedRaw = "0";
			m_percentChainFactor = 0m;
		}

		/// <summary>
		/// 途中式欄・結果欄のテキストを初期化する。
		/// </summary>
		private void ClearTextFields()
		{
			textExpression.Text = "";
			textResult.Text = Constants.Numeric.ZERO_VALUE;
		}

		/// <summary>
		/// 入力・編集状態を示す各種フラグを初期化する。
		/// </summary>
		private void ResetFlags()
		{
			m_textOverwrite = true;
			m_numDot = false;
			m_isErrorState = false;
			m_isClearEntry = false;
			m_lastActionWasPercent = false;
			m_clearedExprAfterEqual = false;
			m_inPercentChainAfterEqual = false;
		}

		/// <summary>
		/// フォントサイズを既定値に戻す。
		/// </summary>
		private void ResetFonts()
		{
			textResult.Font = new Font(textResult.Font.FontFamily, m_defaultFontSize, textResult.Font.Style);
			textExpression.Font = new Font(textExpression.Font.FontFamily, m_defaultExpressionFontSize, textExpression.Font.Style);
		}
	}
}
